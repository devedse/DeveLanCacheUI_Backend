﻿using Microsoft.AspNetCore.SignalR;

namespace DeveLanCacheUI_Backend.Hubs
{
    public class LanCacheHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
