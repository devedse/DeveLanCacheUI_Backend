namespace DeveLanCacheUI_Backend.Services.OriginalDepotEnricher
{
    public class OriginalSteamAppObtainerService : ISteamAppObtainerService
    {
        public App? GetSteamAppById(uint? steamAppId)
        {
            if (steamAppId != null && SteamApi.SteamAppDict.TryGetValue(steamAppId.Value, out var app))
            {
                return app;
            }
            return null;
        }
    }
}
