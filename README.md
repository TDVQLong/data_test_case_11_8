# eShopOnContainersAI 

## Definition and goals

This repo has a forked version of https://github.com/dotnet-architecture/eShopOnContainers which has been evolved by adding AI and ML features.

*eShopOnContainers* is a cloud-native application based on microservices architecture and Docker containers.
*eShopOnContainersAI* is therefore a forked version of eShopOnContainers that is extended with AI features (Machine Learning and Deep Learning) plus a Bot client as a new client app which surfaces all the AI features along with the modified MVC web application.

Main AI/ML technologies used are:

- **ML.NET (Machine Learning .NET)**
- **Azure Cognitive Services (Computer Vision)**
- **TensorFlow / TensorFlowSharp**
- **CNTK**
- **Bot Framework**

Here's a vision of the architecture where the grayed area is what is coming derived from https://github.com/dotnet-architecture/eShopOnContainers and the rest of the diagram is about the new added AI features.

![image](https://user-images.githubusercontent.com/1712635/42792118-bc21b18e-8928-11e8-9084-a5a6af87c8ba.png)

(*) Note that the architecture diagram is currently missing the ML.NET microservice, but the ML.NET scenario is explained in the Wiki.
This diagram will be updated soon.

The following diagram positions the multiple AI technologies per AI function and type:

![image](https://user-images.githubusercontent.com/1222398/36477436-746362e6-1701-11e8-9312-52faecbda715.png)

You will learn how to use Pre-Built models (such as in Cognitive Services), Pre-Trained and Custom models to add AI and ML features into any application:

*	Regression Models: These models are the most well-known and used around any kind of scenarios. Although they are very simple (compared with other models like deep neural networks) they are still the most used around the world. In eShopOnContainersAI we will use regression models to predict future product demand, training the algorithm with the order history data.
*	Recommendation systems: One of the most used cases, recommend products from the basket, will be used as example of these models.
*	Natural Language Processing: Bots are the corner stone of current AI applications. You will learn how to create new solutions based in BOT framework, integrate bots in your current applications, or use L.U.I.S. to get information about user intents, 
*	Computer Vision: These models gained much traction in current decade, and industry is investing large amount of resources in this field. Using different strategies, you will learn how to search for similar images, using Cognitive Services or deploying your own custom trained models.


See Wiki for how set it up and see the multiple scenarios:
https://github.com/dotnet-architecture/eShopOnContainersAI/wiki

## Sending feedback and pull requests
We'd appreciate your feedback, improvements and ideas.
You can create new issues at the issues section, do pull requests and/or send emails to **eshop_feedback@service.microsoft.com**

## System Architecture Overview
Biểu đồ dưới đây mô tả cách hệ thống thương mại điện tử kết hợp Trí Tuệ Nhân Tạo (AI) hoạt động một cách đơn giản nhất.

```mermaid
flowchart TD
    %% Định dạng màu sắc để dễ phân biệt
    classDef user fill:#ffcccc,stroke:#ff6666,stroke-width:2px,color:#000
    classDef frontend fill:#ffe6cc,stroke:#ff9933,stroke-width:2px,color:#000
    classDef gateway fill:#ccffff,stroke:#33cccc,stroke-width:2px,color:#000
    classDef core fill:#ccffcc,stroke:#33cc33,stroke-width:2px,color:#000
    classDef ai fill:#e6ccff,stroke:#9933ff,stroke-width:2px,color:#000
    classDef data fill:#f2f2f2,stroke:#b3b3b3,stroke-width:2px,color:#000

    %% 1. Khách hàng và Người dùng
    subgraph Users [1. Ai sử dụng hệ thống?]
        Customer([Khách hàng mua sắm]):::user
        Manager([Ban Quản lý / Nhân viên]):::user
    end

    %% 2. Các ứng dụng tiếp xúc với người dùng
    subgraph Interfaces [2. Họ dùng qua công cụ nào?]
        direction LR
        Web[Trang Web Mua sắm]:::frontend
        Mobile[Ứng dụng Di động]:::frontend
        Bot[Trợ lý Ảo (Chatbot)]:::frontend
        AdminUI[Trang Quản lý Nội bộ]:::frontend
    end

    %% 3. Hệ thống tiếp nhận
    subgraph Gateways [3. Trạm điều phối thông tin]
        Router[Hệ thống Phân luồng & Kiểm duyệt yêu cầu]:::gateway
    end

    %% 4. Hệ thống cốt lõi và AI
    subgraph Backend [4. Các bộ phận xử lý bên trong]
        direction TB
        
        subgraph Core [Các dịch vụ Bán hàng Cốt lõi]
            direction LR
            Catalog[Cửa hàng & Sản phẩm]:::core
            Basket[Quản lý Giỏ hàng]:::core
            Ordering[Xử lý Đơn hàng]:::core
            User[Tài khoản & Phân quyền]:::core
        end

        subgraph AI [Bộ phận Trí tuệ Nhân tạo - AI]
            direction LR
            Recommend[Gợi ý Sản phẩm thông minh]:::ai
            Search[Tìm kiếm bằng Hình ảnh]:::ai
            Forecast[Dự báo Doanh thu / Xu hướng]:::ai
        end
    end

    %% 5. Lưu trữ
    subgraph Storage [5. Nơi lưu trữ thông tin]
        DB[(Hệ thống Lưu trữ Dữ liệu)]:::data
    end

    %% Các đường kết nối
    Customer --> Web
    Customer --> Mobile
    Customer --> Bot
    Manager --> AdminUI

    Web --> Router
    Mobile --> Router
    Bot --> Router
    AdminUI --> Router

    Router ==>|Chuyển yêu cầu mua sắm| Core
    Router ==>|Yêu cầu tính năng thông minh| AI

    Recommend -. Dựa trên dữ liệu .-> Catalog
    Search -. Dựa trên dữ liệu .-> Catalog
    Forecast -. Dựa trên lịch sử .-> Ordering

    Core ==>|Lưu & Lấy dữ liệu| DB
```

### Giải thích các thành phần:

1. **Ai sử dụng hệ thống?** Hệ thống phục vụ hai nhóm chính là khách hàng (mua sắm) và nhân viên/quản lý (để theo dõi, quản trị).
2. **Họ dùng qua công cụ nào?** Người dùng có thể mua sắm qua điện thoại, web hoặc chat với trợ lý ảo. Quản lý thì có một trang điều khiển riêng.
3. **Trạm điều phối thông tin:** Khi bạn bấm một nút trên ứng dụng, yêu cầu sẽ chạy qua một "Trạm kiểm soát" (Gateway). Trạm này sẽ quyết định xem yêu cầu của bạn thuộc về bộ phận nào để chuyển đi cho chính xác (ví dụ: yêu cầu thanh toán thì chuyển cho bộ phận Đơn hàng).
4. **Các bộ phận xử lý bên trong:** 
   - **Các dịch vụ bán hàng cốt lõi:** Làm những việc truyền thống như quản lý danh sách sản phẩm, giỏ hàng, và tạo đơn hàng.
   - **Bộ phận Trí tuệ Nhân tạo (AI):** Cung cấp trải nghiệm "tương lai" cho khách hàng. Nó học thói quen người dùng để **Gợi ý sản phẩm**, cho phép **Tìm kiếm món đồ bằng cách chụp ảnh**, và giúp cửa hàng **Dự báo được doanh thu** trong tương lai.
5. **Nơi lưu trữ thông tin:** Mọi dữ liệu về tài khoản, đơn hàng hay sản phẩm đều được lưu vào các "két sắt" dữ liệu một cách an toàn.
