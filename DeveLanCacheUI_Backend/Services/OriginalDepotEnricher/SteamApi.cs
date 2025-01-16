namespace DeveLanCacheUI_Backend.Services.OriginalDepotEnricher
{
    public static class SteamApi
    {
        private static readonly Lazy<Dictionary<uint, App>> _steamAppDict = new Lazy<Dictionary<uint, App>>(() => SteamApiData.applist.apps.ToDictionary(t => t.appid, t => t));
        private static readonly Lazy<SteamApiData> _steamApiData = new Lazy<SteamApiData>(LoadSteamApiData);

        public static SteamApiData SteamApiData => _steamApiData.Value;
        public static Dictionary<uint, App> SteamAppDict => _steamAppDict.Value;

        private static SteamApiData LoadSteamApiData()
        {
            using var c = new HttpClient();
            var result = c.GetAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/").Result;
            var resultString = result.Content.ReadAsStringAsync().Result;

            var retval = JsonSerializer.Deserialize<SteamApiData>(resultString);

            //There was one person with an issue where one appid was added as a duplicate???, no idea how but this seems to be a bug in the steam api.
            //I'm just going to distinct on it
            retval.applist.apps = retval.applist.apps.DistinctBy(t => t.appid).ToArray();

            return retval;
        }
    }
}
