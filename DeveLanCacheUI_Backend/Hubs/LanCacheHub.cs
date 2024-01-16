namespace DeveLanCacheUI_Backend.Hubs
{
    public class LanCacheHub : Hub
    {
        public async Task NotifyChanges()
        {
            await Clients.All.SendAsync("UpdateDownloadEvents");
        }
    }
}
