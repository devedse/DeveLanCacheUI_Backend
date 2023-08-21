namespace DeveLanCacheUI_Backend.Services.OriginalDepotEnricher
{
    public class OriginalSteamAppObtainerService : ISteamAppObtainerService
    {
        public App? GetSteamAppById(uint? steamAppId)
        {
            if (steamAppId == null)
            {
                return null;
            }
            var app = SteamApi.SteamApiData?.applist?.apps?.FirstOrDefault(t => t?.appid == steamAppId);
            return app;
        }
    }
}
