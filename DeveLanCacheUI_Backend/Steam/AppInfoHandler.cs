namespace DeveLanCacheUI_Backend.Steam
{
    /// <summary>
    /// Responsible for retrieving application metadata from Steam
    ///
    /// Adapted from : https://github.com/tpill90/steam-lancache-prefill/blob/master/SteamPrefill/Handlers/AppInfoHandler.cs
    /// </summary>
    public class AppInfoHandler
    {
        private readonly Steam3Session _steam3Session;

        /// <summary>
        /// A dictionary of all app metadata currently retrieved from Steam
        /// </summary>
        public ConcurrentDictionary<uint, AppInfo> LoadedAppInfos { get; } = new ConcurrentDictionary<uint, AppInfo>();

        public AppInfoHandler(Steam3Session steam3Session)
        {
            _steam3Session = steam3Session;
        }

        /// <summary>
        /// Gets the latest app metadata from steam, for the specified apps, as well as their related DLC apps
        /// </summary>
        public async Task<List<AppInfo>> RetrieveAppMetadataAsync(List<uint> appIds)
        {
            await BulkLoadAppInfoAsync(appIds);

            // Once we have loaded all the apps, we can also load information for related DLC
            await FetchDlcAppInfoAsync();

            return appIds.Select(appId => GetAppInfo(appId))
                         // These are unknown app ids
                         .Where(e => e != null)
                         .ToList();
        }

        private async Task BulkLoadAppInfoAsync(List<uint> appIds)
        {
            var filteredAppIds = appIds.Where(e => !LoadedAppInfos.ContainsKey(e))
                                                .Distinct()
                                                .ToList();

            // Breaking into at most 10 concurrent batches
            int batchSize = (filteredAppIds.Count / 5) + 1;
            var batches = filteredAppIds.Chunk(batchSize).ToList();

            // Breaking the request into smaller batches that complete faster
            var batchJobs = new List<Task>();
            foreach (var batch in batches)
            {
                batchJobs.Add(AppInfoRequestAsync(batch.ToList()));
            }

            await Task.WhenAll(batchJobs);
            //_ansiConsole.LogMarkupVerbose($"Loaded metadata for {Magenta(filteredAppIds.Count)} apps", initialAppIdLoadTimer);
        }

        /// <summary>
        /// Retrieves the latest AppInfo for multiple apps at the same time.  One large request containing multiple apps is significantly faster
        /// than multiple individual requests, as it seems that there is a minimum threshold for how quickly steam will return results.
        /// </summary>
        /// <param name="appIdsToLoad">The list of App Ids to retrieve info for</param>
        private async Task AppInfoRequestAsync(List<uint> appIdsToLoad)
        {
            if (!appIdsToLoad.Any())
            {
                return;
            }

            // Some apps will require an additional "access token" in order to retrieve their app metadata
            var accessTokensResponse = await _steam3Session.SteamAppsApi.PICSGetAccessTokens(appIdsToLoad, new List<uint>()).ToTask();
            var appTokens = accessTokensResponse.AppTokens;

            // Build out the requests
            var requests = new List<SteamApps.PICSRequest>();
            foreach (var appId in appIdsToLoad)
            {
                var request = new SteamApps.PICSRequest(appId);
                if (appTokens.ContainsKey(appId))
                {
                    request.AccessToken = appTokens[appId];
                }
                requests.Add(request);
            }

            // Finally request the metadata from steam
            var resultSet = await _steam3Session.SteamAppsApi.PICSGetProductInfo(requests, new List<SteamApps.PICSRequest>()).ToTask();

            List<PicsProductInfo> appInfos = resultSet.Results.SelectMany(e => e.Apps)
                                                      .Select(e => e.Value)
                                                      .ToList();
            foreach (var app in appInfos)
            {
                LoadedAppInfos.TryAdd(app.ID, new AppInfo(app.ID, app.KeyValues));
            }

            // Handling unknown apps
            List<uint> unknownAppIds = resultSet.Results.SelectMany(e => e.UnknownApps).ToList();
            foreach (var unknownAppId in unknownAppIds)
            {
                LoadedAppInfos.TryAdd(unknownAppId, new AppInfo(unknownAppId, "Unknown"));
            }
        }

        /// <summary>
        /// Steam stores all DLCs for a game as separate "apps", so they must be loaded after the game's AppInfo has been retrieved,
        /// and the list of DLC AppIds are known.
        ///
        /// Once the DLC apps are loaded, the final combined depot list (both the app + dlc apps) will be built.
        /// </summary>
        private async Task FetchDlcAppInfoAsync()
        {
            var dlcAppIds = LoadedAppInfos.Values.SelectMany(e => e.DlcAppIds).ToList();
            var containingAppIds = LoadedAppInfos.Values.Where(e => e.Type == AppType.Game)
                                                 .SelectMany(e => e.Depots)
                                                 .Select(e => e.ContainingAppId)
                                                 .ToList();

            var idsToLoad = containingAppIds.Union(dlcAppIds).ToList();
            await BulkLoadAppInfoAsync(idsToLoad);

            // Builds out the list of all depots for each game, including depots from all related DLCs
            // DLCs are stored as separate "apps", so their info comes back separately.
            foreach (var app in LoadedAppInfos.Values.Where(e => e.Type == AppType.Game))
            {
                foreach (var dlcAppId in app.DlcAppIds)
                {
                    var dlcApp = GetAppInfo(dlcAppId);
                    var dlcDepots = dlcApp.Depots;
                    app.Depots.AddRange(dlcDepots);

                    // Clear out the dlc app's depots so that they dont get duplicates added 
                    dlcDepots.Clear();
                }

                var distinctDepots = app.Depots.DistinctBy(e => e.DepotId).ToList();
                app.Depots.Clear();
                app.Depots.AddRange(distinctDepots);
            }
        }

        /// <summary>
        /// Will return an AppInfo for the specified AppId, that contains various metadata about the app.
        /// If the information for the specified app hasn't already been retrieved, then a request to the Steam network will be made.
        /// </summary>
        public virtual AppInfo GetAppInfo(uint appId)
        {
            return LoadedAppInfos[appId];
        }

        public void ClearCachedAppInfos()
        {
            LoadedAppInfos.Clear();
        }
    }
}