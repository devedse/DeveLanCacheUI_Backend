namespace DeveLanCacheUI_Backend.Steam.SteamAppApi
{
    public static class SteamApi
    {
        private static readonly Lazy<Dictionary<int, App>> _steamAppDict = new Lazy<Dictionary<int, App>>(() => SteamApiData.applist.apps.ToDictionary(t => t.appid, t => t));
        private static readonly Lazy<SteamApiData> _steamApiData = new Lazy<SteamApiData>(LoadSteamApiData);

        public static SteamApiData SteamApiData => _steamApiData.Value;
        public static Dictionary<int, App> SteamAppDict => _steamAppDict.Value;

        private static SteamApiData LoadSteamApiData()
        {
            var subDir = "Steam";
            string path = Path.Combine(subDir, "SteamData.json");
            if (File.Exists(path))
            {
                Console.WriteLine($"Found {path} so reading apps from that file.");
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<SteamApiData>(json);
            }
            else
            {
                Console.WriteLine($"Could not find {path}, so obtaining new SteamApi Data...");
                using var c = new HttpClient();
                var result = c.GetAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/").Result;
                var resultString = result.Content.ReadAsStringAsync().Result;
                Console.WriteLine($"Writing result to file. First 1000 chars: {resultString.Substring(0, 1000)}");

                if (!Directory.Exists(subDir))
                {
                    Directory.CreateDirectory(subDir);
                }

                File.WriteAllText(path, resultString);
                return JsonSerializer.Deserialize<SteamApiData>(resultString);
            }
        }
    }
}
