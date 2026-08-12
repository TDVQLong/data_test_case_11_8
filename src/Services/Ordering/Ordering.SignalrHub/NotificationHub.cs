using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordering.SignalrHub
{
    [Authorize]
    public class NotificationsHub : Hub
    {
        // WEBSOCKET LEAK BUG: Intentional RAM leak to accelerate OOM when half-open connections accumulate
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> _leakage = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();

        public override async Task OnConnectedAsync()
        {
            // Allocate 1MB per connection. Since they never timeout, RAM will grow continuously for half-open connections.
            _leakage.TryAdd(Context.ConnectionId, new byte[1024 * 1024]);

            await Groups.AddToGroupAsync(Context.ConnectionId, Context.User.Identity.Name);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            // We intentionally do not remove from _leakage to simulate memory held by zombie connections 
            // OR we can let it remove, but since half-open connections never timeout, this method is never called.
            _leakage.TryRemove(Context.ConnectionId, out _);

            await Groups.AddToGroupAsync(Context.ConnectionId, Context.User.Identity.Name);
            await base.OnDisconnectedAsync(ex);
        }
    }
}
