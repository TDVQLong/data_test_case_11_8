# BÁO CÁO THIẾT LẬP KỊCH BẢN LỖI (CHAOS ENGINEERING) CHO MÔ HÌNH AI

Tài liệu này tổng hợp chi tiết 6 kịch bản lỗi hệ thống phân tán đã được tiêm (inject) vào dự án eShopOnContainersAI. Các kịch bản này phục vụ mục đích cung cấp dữ liệu huấn luyện (Training Data) chất lượng cao cho mô hình AI trong việc giám sát, phát hiện bất thường và tìm kiếm nguyên nhân gốc rễ (Root Cause Analysis).

---

## 1. Kịch bản: Bão Retry và "Đàn bò hoảng loạn" (Retry Storm & Thundering Herd)

### 1.1. Mô tả Kịch bản
- **Tình trạng:** Dịch vụ `catalog.api` bị chậm nhẹ (độ trễ tăng từ 50ms lên 500ms).
- **Phản ứng dây chuyền:** Các Gateway (như `webshoppingapigw`) nhận thấy chậm nên kích hoạt cơ chế Retry (gọi lại nhiều lần).
- **Kết quả:** Lưu lượng truy cập (traffic) đập vào `catalog.api` đột ngột tăng gấp 3-4 lần khiến dịch vụ này chết hẳn. Khi `catalog.api` vừa hồi sinh (restart), toàn bộ các Gateway và Client đồng loạt dồn kết nối vào cùng một tích tắc (Thundering Herd) khiến nó lại tiếp tục sập.

### 1.2. Giá trị Huấn luyện AI
- Hệ thống giám sát (Monitoring) sẽ phát hiện lỗi **503 HTTP** ở tầng Gateway trước khi thấy lỗi ở Catalog.
- **Mục tiêu AI:** Nhận diện được nguyên nhân gốc không nằm ở Gateway mà do chính sách Retry chưa tối ưu kết hợp với sự suy giảm hiệu năng nhẹ ở tầng backend.

### 1.3. Chi tiết Tiêm lỗi (Fault Injection)
- **Gây trễ cho `catalog.api`:**
  - **Tệp sửa đổi:** `CatalogController.cs`
  - **Chi tiết:** Thêm `await Task.Delay(500);` vào đầu các API chính (`Items`, `GetItemById`) để mô phỏng dịch vụ phản hồi trễ > 500ms.
- **Cấu hình bão Retry tại Gateway (`webshoppingagg`):**
  - **Tệp sửa đổi:** `Startup.cs`
  - **Chi tiết:** Thiết lập Timeout cho HttpClient là `200ms` (luôn gây lỗi TimeoutRejectedException do Catalog trễ 500ms). Cấu hình gọi lại ngay lập tức (delay = 0ms) lên đến 10 lần liên tục (`WaitAndRetryAsync(10)`).

---

## 2. Kịch bản: "Tin nhắn độc" (Poison Message) trong Event Bus

### 2.1. Mô tả Kịch bản
- **Tình trạng:** `ordering.api` phát đi sự kiện "Đã tạo đơn hàng". Dịch vụ `marketing.api` nhận được để xử lý chiến dịch, nhưng do code bị lỗi không thể phân tích (parse) được dữ liệu.
- **Phản ứng dây chuyền:** `marketing.api` ném lỗi và đẩy message đó lại vào hàng đợi (Queue) để thử lại một cách vô tận.
- **Kết quả:** Message này kẹt ở đầu hàng đợi, chiếm dụng toàn bộ CPU và RAM của `marketing.api` để xử lý lặp lại. Các sự kiện mua hàng khác ở phía sau bị nghẽn (Head-of-line blocking).

### 2.2. Giá trị Huấn luyện AI
- Ở góc nhìn người dùng, khách hàng vẫn đặt hàng bình thường (vì là luồng bất đồng bộ), nhưng hệ thống Marketing bị đóng băng.
- **Mục tiêu AI:** Học cách liên kết việc **"Tăng vọt CPU ở 1 service"** với Metric **"Độ trễ của RabbitMQ Queue tăng vọt"**.

### 2.3. Chi tiết Tiêm lỗi (Fault Injection)
- **Tạo vòng lặp vô tận ở tầng EventBus:**
  - **Tệp sửa đổi:** `EventBusRabbitMQ.cs`
  - **Chi tiết:** Sửa hàm `CreateConsumerChannel()`. Khi xử lý có lỗi, thay vì vứt bỏ tin nhắn (Dead-letter), gọi `channel.BasicNack` với cờ `requeue: true` ép RabbitMQ đẩy ngược tin nhắn lên đầu hàng đợi liên tục.
- **Tạo lỗi Parse Data trong Marketing API:**
  - **Tệp thêm mới:** `OrderStartedIntegrationEventHandler.cs`
  - **Chi tiết:** Cố tình ném lỗi `FormatException` khi nhận sự kiện tạo đơn hàng để kích hoạt vòng lặp lỗi.

---

## 3. Kịch bản: Hiệu ứng "Hot Key" trong Redis do Mô hình AI Gợi ý

### 3.1. Mô tả Kịch bản
- **Tình trạng:** Mô hình `ai.productrecommender` bị thiên lệch dữ liệu (Model Drift), bất ngờ gợi ý một sản phẩm duy nhất (ví dụ: Item ID = 1) cho tất cả mọi người dùng.
- **Phản ứng dây chuyền:** Toàn bộ khách hàng đều nhấn vào xem cùng một sản phẩm đó.
- **Kết quả:** Cache Redis bị quá tải tại một phân vùng (partition) duy nhất đang lưu trữ ID của sản phẩm đó, tạo ra hiện tượng Hot Key.

### 3.2. Giá trị Huấn luyện AI
- Tài nguyên tổng thể của Redis vẫn ở mức thấp, nhưng chỉ có một Node bị quá tải (100% CPU).
- **Mục tiêu AI:** Nhận diện được lỗi có nguồn gốc từ "hành vi của mô hình AI" làm thay đổi luồng tương tác người dùng, gián tiếp gây ra thắt cổ chai phần cứng.

### 3.3. Chi tiết Tiêm lỗi (Fault Injection)
- **Gây thiên lệch cho Mô hình AI:**
  - **Tệp sửa đổi:** `ProductRecommenderController.cs` và `CatalogAIController.cs`
  - **Chi tiết:** Vô hiệu hóa việc gọi AI thực tế, cấu hình phần cứng hóa (hardcode) API luôn trả về duy nhất ID sản phẩm `[1]`.

---

## 4. Kịch bản: Cạn kiệt Thread Pool ở API Gateway

### 4.1. Mô tả Kịch bản
- **Tình trạng:** Dịch vụ nhận diện hình ảnh (`ai.productsearchimagebased`) bị lỗi GPU, phải chuyển sang CPU khiến thời gian phản hồi tăng từ 1s lên 10s.
- **Phản ứng dây chuyền:** API Gateway (`webaiapigw`) nhận request và phải chờ (block thread) API trả lời.
- **Kết quả:** Chỉ trong vài phút, toàn bộ Thread Pool của Gateway bị cạn kiệt. Dù người dùng gọi một API rất nhẹ (như lấy danh sách sản phẩm) qua Gateway này cũng sẽ bị Timeout.

### 4.2. Giá trị Huấn luyện AI
- Dịch vụ Catalog hoàn toàn khỏe mạnh, nhưng người dùng báo lỗi hệ thống sập.
- **Mục tiêu AI:** Phân tích cấu trúc Topology để hiểu rằng một thành phần cực chậm (AI API) đã gây hiệu ứng nghẽn cổ chai làm sập trạm trung chuyển (Gateway), qua đó làm gián đoạn mọi dịch vụ khác.

### 4.3. Chi tiết Tiêm lỗi (Fault Injection)
- **Giả lập AI sụt giảm hiệu năng:**
  - **Tệp sửa đổi:** `ProductSearchImageBasedController.cs`
  - **Chi tiết:** Thêm `Thread.Sleep(10000)` vào API `classifyImage` để giữ luồng chờ trong 10 giây.
- **Giới hạn Thread Pool và gây Block ở Gateway:**
  - **Tệp sửa đổi:** `Program.cs` và `Startup.cs` của `ApiGw-Base`
  - **Chi tiết:** Giới hạn `ThreadPool.SetMaxThreads(10, 10)`. Thêm middleware dùng hàm `.Wait()` (Sync-over-Async) để bắt Gateway phải "đóng băng" tài nguyên Thread khi chờ AI.

---

## 5. Kịch bản: Rò rỉ kết nối WebSocket ẩn (SignalR Slow Consumer)

### 5.1. Mô tả Kịch bản
- **Tình trạng:** Một bản cập nhật trên Mobile bị lỗi khiến App không bao giờ đóng kết nối WebSocket cũ khi mạng chập chờn.
- **Phản ứng dây chuyền:** Dịch vụ `ordering.signalrhub` tích tụ hàng chục ngàn kết nối ảo (Half-open connections).
- **Kết quả:** Hệ thống ngầm tiêu thụ hết RAM và bất ngờ bị orchestrator (Docker/K8s) ngắt bắt buộc (OOM Killed).

### 5.2. Giá trị Huấn luyện AI
- Số lượng request HTTP có thể rất thấp, nhưng lượng TCP Socket (Established/Close_Wait) lại tăng phi mã.
- **Mục tiêu AI:** Nhận diện mẫu (pattern) "Rò rỉ tài nguyên tăng dần" (Leakage) để cảnh báo sớm trước khi dịch vụ bị hệ thống Kill.

### 5.3. Chi tiết Tiêm lỗi (Fault Injection)
- **Vô hiệu hóa cơ chế dọn dẹp kết nối (Timeout):**
  - **Tệp sửa đổi:** `Startup.cs` (`Ordering.SignalrHub`)
  - **Chi tiết:** Tăng `ClientTimeoutInterval` và `KeepAliveInterval` lên 365 ngày để hệ thống dung túng mọi kết nối chết.
- **Giả lập rò rỉ RAM (Memory Leak):**
  - **Tệp sửa đổi:** `NotificationHub.cs`
  - **Chi tiết:** Mỗi khi có kết nối mới `OnConnectedAsync`, ép hệ thống lưu giữ vĩnh viễn mảng byte 1MB trên RAM, làm tăng tốc độ Out Of Memory.

---

## 6. Kịch bản: Lỗi toàn vẹn dữ liệu phân tán (Distributed Saga Failure)

### 6.1. Mô tả Kịch bản
- **Tình trạng:** Khách hàng tiến hành thanh toán thành công, hệ thống gọi Catalog để trừ tồn kho.
- **Phản ứng dây chuyền:** Giao dịch lưu Database thành công, nhưng đúng lúc đó mạng kết nối tới RabbitMQ bị đứt 1 nhịp. Sự kiện để tiếp tục quy trình (Saga) không được sinh ra.
- **Kết quả:** Đơn hàng nằm mãi ở trạng thái "Đang chờ xử lý". Khách bị trừ tiền nhưng quy trình gãy đoạn.

### 6.2. Giá trị Huấn luyện AI
- Không có bất kỳ service nào báo lỗi "Sập". Đây là dạng **"Lỗi câm" (Silent Failure)**.
- **Mục tiêu AI:** Huấn luyện trên hệ thống Distributed Tracing (Trace ID) để phát hiện một Transaction (Saga) đã bắt đầu nhưng không bao giờ ghi nhận Event kết thúc.

### 6.3. Chi tiết Tiêm lỗi (Fault Injection)
- **Làm đứt gãy luồng Saga:**
  - **Tệp sửa đổi:** `OrderStatusChangedToAwaitingValidationIntegrationEventHandler.cs`
  - **Chi tiết:** Ẩn (comment out) dòng mã `PublishThroughEventBusAsync`. DB vẫn lưu trạng thái thành công nhưng sự kiện đẩy luồng quy trình đi tiếp bị "nuốt" hoàn toàn, khiến đơn hàng kẹt vô thời hạn.

> **💡 Khuyến nghị cho công đoạn tiếp theo:** Để tạo bộ Dataset đa dạng cho AI, hãy sử dụng các công cụ Chaos Engineering (như Chaos Mesh, Gremlin) kết hợp với các kịch bản trên để tự động hóa việc tiêm lỗi như độ trễ mạng (Network Latency), ngắt kết nối (Network Partition), hoặc bóp băng thông CPU.
