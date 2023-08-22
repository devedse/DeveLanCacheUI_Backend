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
        //public ConcurrentDictionary<uint, AppInfo> LoadedAppInfos { get; } = new ConcurrentDictionary<uint, AppInfo>();

        public AppInfoHandler(Steam3Session steam3Session)
        {
            _steam3Session = steam3Session;
        }

        ///// <summary>
        ///// Gets the latest app metadata from steam, for the specified apps, as well as their related DLC apps
        ///// </summary>
        //public async Task<List<AppInfo>> RetrieveAppMetadataAsync(List<uint> appIds)
        //{
        //    await BulkLoadAppInfoAsync(appIds);

        //    //// Once we have loaded all the apps, we can also load information for related DLC
        //    //await FetchDlcAppInfoAsync();

        //    return appIds.Select(appId => GetAppInfo(appId))
        //                 // These are unknown app ids
        //                 .Where(e => e != null)
        //                 .ToList();
        //}

        public async Task<List<SteamDepotEnricherCSVModel>> BulkLoadAppInfoAsync(List<uint> appIds)
        {
            return await AppInfoRequestAsync(appIds);
            //var filteredAppIds = appIds.Distinct().ToList();

            //// Breaking into at most 10 concurrent batches
            //int batchSize = (filteredAppIds.Count / 5) + 1;
            //var batches = filteredAppIds.Chunk(batchSize).ToList();

            //var total = new List<SteamDepotEnricherCSVModel>();
            //foreach (var batch in batches)
            //{
            //    var res = await AppInfoRequestAsync(batch.ToList());
            //    total.AddRange(res);
            //}

            //return total;

            //// Breaking the request into smaller batches that complete faster
            //var batchJobs = new List<Task>();
            //foreach (var batch in batches)
            //{
            //    batchJobs.Add(AppInfoRequestAsync(batch.ToList()));
            //}

            //await Task.WhenAll(batchJobs);
            //_ansiConsole.LogMarkupVerbose($"Loaded metadata for {Magenta(filteredAppIds.Count)} apps", initialAppIdLoadTimer);
        }

        /// <summary>
        /// Retrieves the latest AppInfo for multiple apps at the same time.  One large request containing multiple apps is significantly faster
        /// than multiple individual requests, as it seems that there is a minimum threshold for how quickly steam will return results.
        /// </summary>
        /// <param name="appIdsToLoad">The list of App Ids to retrieve info for</param>
        private async Task<List<SteamDepotEnricherCSVModel>> AppInfoRequestAsync(List<uint> appIdsToLoad)
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

        ///// <summary>
        ///// Will return an AppInfo for the specified AppId, that contains various metadata about the app.
        ///// If the information for the specified app hasn't already been retrieved, then a request to the Steam network will be made.
        ///// </summary>
        //public virtual AppInfo GetAppInfo(uint appId)
        //{
        //    return LoadedAppInfos[appId];
        //}

        //public void ClearCachedAppInfos()
        //{
        //    LoadedAppInfos.Clear();
        //}
    }
}