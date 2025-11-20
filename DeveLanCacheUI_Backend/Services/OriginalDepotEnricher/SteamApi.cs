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
            // Pull steam-applist.json(.gz) from latest GitHub release (Steam GetAppList endpoint broken).
            var bytes = RemoteFileDownloader.DownloadGithubLatestReleaseAssetAsync(
                owner: "devedse",
                repo: "DeveLanCacheUI_SteamDepotFinder_Runner",
                assetName: "steam-applist.json.gz",
                cancellationToken: CancellationToken.None
            ).Result;

            var resultString = Encoding.UTF8.GetString(bytes);
            
            // Deserialize from GitHub format
            var githubData = JsonSerializer.Deserialize<GitHubSteamApiData>(resultString);
            
            if (githubData?.response?.apps == null)
            {
                throw new FileNotFoundException("Could not download / parse / find the steam-applist.json.gz file from DeveLanCacheUI_SteamDepotFinder_Runner");
            }
            
            // Convert to original SteamApiData format
            var retval = new SteamApiData
            {
                applist = new Applist
                {
                    apps = githubData.response.apps
                        .Select(a => new App { appid = a.appid, name = a.name })
                        .DistinctBy(t => t.appid)
                        .ToArray()
                }
            };
            
            return retval;

            // OLD FALLBACK (commented out intentionally):
            // using var c = new HttpClient();
            // var result = c.GetAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/").Result;
            // var resultString = result.Content.ReadAsStringAsync().Result;
            // var retval = JsonSerializer.Deserialize<SteamApiData>(resultString) ?? new SteamApiData { applist = new Applist { apps = Array.Empty<App>() } };
            // retval.applist.apps = retval.applist.apps.DistinctBy(t => t.appid).ToArray();
            // return retval;
        }





        // Classes for deserializing from GitHub release JSON format
        private class GitHubSteamApiData
        {
            public GitHubSteamResponse response { get; set; }
        }

        private class GitHubSteamResponse
        {
            public GitHubSteamApp[] apps { get; set; }
        }

        private class GitHubSteamApp
        {
            public uint appid { get; set; }
            public string name { get; set; }
            public long last_modified { get; set; }
            public long price_change_number { get; set; }
        }
    }
}
