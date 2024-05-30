namespace DeveLanCacheUI_Backend.Steam
{
    /// <summary>
    /// Responsible for retrieving application metadata from Steam
    ///
    /// Adapted from : https://github.com/tpill90/steam-lancache-prefill/blob/master/SteamPrefill/Handlers/AppInfoHandler.cs
    /// </summary>
    public class AppInfoHandler : ISteamAppObtainerService
    {
        private readonly Steam3Session _steam3Session;
        private readonly ILogger<AppInfoHandler> _logger;

        public AppInfoHandler(Steam3Session steam3Session, ILogger<AppInfoHandler> logger)
        {
            _steam3Session = steam3Session;
            _logger = logger;
        }

        private Dictionary<uint, App> _cachedAppNames = new Dictionary<uint, App>();

        public App? GetSteamAppById(uint? steamAppId)
        {
            if (steamAppId != null && _cachedAppNames.TryGetValue(steamAppId.Value, out var app))
            {
                return app;
            }
            return null;
        }

        //TODO comment
        public async Task<List<App>> EnsureAppsAreLoaded()
        {
            _logger.LogInformation("Retrieving all known AppIds");

            using var steamAppsApi = _steam3Session.Configuration.GetAsyncWebAPIInterface("ISteamApps");
            var response = await steamAppsApi.CallAsync(HttpMethod.Get, "GetAppList", 2);

            var apiApps = response["apps"].Children.Select(app =>
                new App()
                {
                    appid = app["appid"].AsUnsignedInteger(),
                    name = app["name"].AsString() ?? "Unknown"
                }
                ).ToList();

            _cachedAppNames = apiApps.DistinctBy(t => t.appid).ToDictionary(t => t.appid, t => t);

            _logger.LogInformation("Retrieved {appCount} apps", apiApps.Count);
            return apiApps;
        }

        public async Task<List<App>> RetrieveAllAppIds2()
        {
            return _cachedAppNames.Values.ToList();
        }

        /// <summary>
        /// Retrieves the latest AppInfo for multiple apps at the same time.  One large request containing multiple apps is significantly faster
        /// than multiple individual requests, as it seems that there is a minimum threshold for how quickly steam will return results.
        /// </summary>
        /// <param name="appIdsToLoad">The list of App Ids to retrieve info for</param>
        public async Task<List<SteamDepotEnricherCSVModel>> BulkLoadAppInfoAsync(List<uint> appIdsToLoad)
        {
            if (!appIdsToLoad.Any())
            {
                return new List<SteamDepotEnricherCSVModel>();
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

            var appInfos = resultSet.Results.SelectMany(e => e.Apps).ToList();

            var toReturn = new List<SteamDepotEnricherCSVModel>();

            foreach (var a in appInfos)
            {
                var depots = a.Value.KeyValues["depots"];

                var updatedApp = new App()
                {
                    appid = a.Value.KeyValues["appid"].AsUnsignedInteger(),
                    name = a.Value.KeyValues["common"]?["name"]?.AsString() ?? "Unknown"
                };

                _cachedAppNames[updatedApp.appid] = updatedApp;

                foreach (var dep in depots.Children)
                {
                    if (uint.TryParse(dep.Name, out var depotUint) && dep.Value == null)
                    {
                        if (dep.Children.Any(t => t.Name == "depotfromapp"))
                        {
                            var depfromappString = dep.Children.First(t => t.Name == "depotfromapp").AsString();

                            //Some apps have some strange characters in the depot id's: https://steamdb.info/app/1106980/depots/
                            var depfromappStringNumberified = new string(depfromappString?.Where(t => char.IsDigit(t)).ToArray());
                            var worked2 = uint.TryParse(depfromappStringNumberified, out var depfromapp);

                            //Assume that if depfromapp == 0, it's a redistributable that we've already obtained elsewhere
                            //Example: https://steamdb.info/app/2203540/depots/
                            if (worked2 && depfromapp != 0)
                            {
                                //var worked3 = SteamApi.SteamAppDict.TryGetValue(depfromapp, out var appNameThing2);
                                //string appName2 = worked3 ? appNameThing2!.name : "unknown";

                                //var outputString = ToOutputStringSanitized(depfromappStringNumberified, appName2, dep.Name);

                                var csv = new SteamDepotEnricherCSVModel()
                                {
                                    SteamAppId = depfromapp,
                                    SteamAppName = "",
                                    SteamDepotId = depotUint
                                };

                                toReturn.Add(csv);
                            }
                        }
                        else
                        {
                            var csv = new SteamDepotEnricherCSVModel()
                            {
                                SteamAppId = a.Key,
                                SteamAppName = a.Value.KeyValues["common"]?["name"]?.AsString() ?? "",
                                SteamDepotId = depotUint
                            };

                            toReturn.Add(csv);
                        }
                    }
                }
            }

            //This cleans all callbacks. If we don't do this loading all apps takes around 7gb and never cleans up
            _steam3Session.CallbackManager.RunWaitAllCallbacks(timeout: TimeSpan.FromMilliseconds(0));

            //// Handling unknown apps
            //List<uint> unknownAppIds = resultSet.Results.SelectMany(e => e.UnknownApps).ToList();
            //foreach (var unknownAppId in unknownAppIds)
            //{
            //    LoadedAppInfos.TryAdd(unknownAppId, new AppInfo(unknownAppId, "Unknown"));
            //}

            var toReturn2 = toReturn
                .GroupBy(p => new { p.SteamAppId, p.SteamDepotId })
                .Select(g => g.First())
                .ToList();

            return toReturn2;
        }

        ///// <summary>
        ///// Steam stores all DLCs for a game as separate "apps", so they must be loaded after the game's AppInfo has been retrieved,
        ///// and the list of DLC AppIds are known.
        /////
        ///// Once the DLC apps are loaded, the final combined depot list (both the app + dlc apps) will be built.
        ///// </summary>
        //private async Task FetchDlcAppInfoAsync()
        //{
        //    var dlcAppIds = LoadedAppInfos.Values.SelectMany(e => e.DlcAppIds).ToList();
        //    var containingAppIds = LoadedAppInfos.Values.Where(e => e.Type == AppType.Game)
        //                                         .SelectMany(e => e.Depots)
        //                                         .Select(e => e.ContainingAppId)
        //                                         .ToList();

        //    var idsToLoad = containingAppIds.Union(dlcAppIds).ToList();
        //    await BulkLoadAppInfoAsync(idsToLoad);

        //    // Builds out the list of all depots for each game, including depots from all related DLCs
        //    // DLCs are stored as separate "apps", so their info comes back separately.
        //    foreach (var app in LoadedAppInfos.Values.Where(e => e.Type == AppType.Game))
        //    {
        //        foreach (var dlcAppId in app.DlcAppIds)
        //        {
        //            var dlcApp = GetAppInfo(dlcAppId);
        //            var dlcDepots = dlcApp.Depots;
        //            app.Depots.AddRange(dlcDepots);

        //            // Clear out the dlc app's depots so that they dont get duplicates added 
        //            dlcDepots.Clear();
        //        }

        //        var distinctDepots = app.Depots.DistinctBy(e => e.DepotId).ToList();
        //        app.Depots.Clear();
        //        app.Depots.AddRange(distinctDepots);
        //    }
        //}
    }
}