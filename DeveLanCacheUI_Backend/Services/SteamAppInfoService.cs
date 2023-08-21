namespace DeveLanCacheUI_Backend.Services
{
    //TODO document what this is for
    public class SteamAppInfoService : BackgroundService
    {
        private readonly ILogger<SteamAppInfoService> _logger;
        private readonly Steam3Session _steam3Session;
        private readonly AppInfoHandler _appInfoHandler;
        private readonly IServiceProvider _services;

        private uint _currentChangeNumber;

        public SteamAppInfoService(ILogger<SteamAppInfoService> logger, Steam3Session steam3Session, AppInfoHandler appInfoHandler,
            IServiceProvider services)
        {
            _logger = logger;
            _steam3Session = steam3Session;
            _appInfoHandler = appInfoHandler;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _steam3Session.LoginToSteam();

            await using (var scope = _services.CreateAsyncScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();

                var currentChangeNumberString = dbContext.Settings.FirstOrDefault(t => t.Key == DbSetting.SettingKey_SteamChangeNumber)?.Value;
                if (currentChangeNumberString != null)
                {
                    _currentChangeNumber = uint.Parse(currentChangeNumberString);
                }
            }



            //await ProcessAllKnownAppIds();

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            await WatchForAppUpdates(stoppingToken);
        }

        /* TODO handle the "task was cancelled" and "key not found in dictionary" exceptions more gracefully.
            They don't actually hurt anything, as this code will automatically recover, but they do look concerning in the logs
         */
        //private async Task ProcessAllKnownAppIds()
        //{


        //    // Get all currently known (or unknown) app ids from Steam
        //    List<uint> allKnownAppIds = await RetrieveAllAppIds2();


        //    // Filter out any app ids that have been previously processed
        //    //var previouslyProcessedIds = PreviouslyProcessedAppIds();
        //    //var appIdsToProcess = allKnownAppIds.Where(e => !previouslyProcessedIds.Contains(e))
        //    //                                    .OrderBy(e => e)
        //    //                                    .ToList();
        //    var appIdsToProcess = allKnownAppIds;

        //    var chunkSize = 1000;
        //    var batches = appIdsToProcess.Chunk(chunkSize).ToList();

        //    _logger.LogInformation("Retrieving app metadata...");
        //    while (batches.Any())
        //    {
        //        try
        //        {
        //            _logger.LogInformation("{remainingCount} apps remaining", batches.Sum(e => e.Length).ToString("N0"));
        //            var currentBatch = batches.First().ToList();

        //            // Retrieving app info
        //            var appInfos = await _appInfoHandler.RetrieveAppMetadataAsync(currentBatch);

        //            //// Persisting to DB
        //            //using var scope = _services.CreateAsyncScope();
        //            //using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
        //            //var models = appInfos.Select(e => new DbSteamAppInfo
        //            //{
        //            //    AppId = e.AppId,
        //            //    Name = e.Name ?? "",
        //            //    Depots = e.Depots.Select(e => new DbSteamDepot
        //            //    {
        //            //        Id = e.DepotId
        //            //    }).ToList()
        //            //});
        //            //dbContext.AddRange(models);
        //            //dbContext.SaveChanges();

        //            // If successful pop it from the list of jobs
        //            batches.RemoveAt(0);
        //        }
        //        catch (Exception e)
        //        {
        //            _logger.LogError(e, "Unexpected error while retrieving app info");
        //        }
        //    }

        //    //_appInfoHandler.ClearCachedAppInfos();
        //    _logger.LogInformation("Done loading app metadata");
        //}

        private async Task WatchForAppUpdates(CancellationToken stoppingToken)
        {
            uint previousChangeNumber = 0;
            _logger.LogInformation("Loading latest app changes....");
            //await Task.Delay(60_000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                List<uint> changedApps = new List<uint>();
                if (_currentChangeNumber == 0)
                {
                    var picsChangesResult = await _steam3Session.SteamAppsApi.PICSGetChangesSince().ToTask();
                    var currentChangeNumber = picsChangesResult.CurrentChangeNumber;
                    _currentChangeNumber = currentChangeNumber;
                    changedApps = (await RetrieveAllAppIds2()).Select(t => t.appid).ToList();
                }
                else
                {
                    var result = await _steam3Session.SteamAppsApi.PICSGetChangesSince(previousChangeNumber).ToTask();
                    _currentChangeNumber = result.CurrentChangeNumber;
                    changedApps = result.AppChanges.Select(e => e.Value.ID).ToList();
                }

                if (changedApps.Any())
                {
                    _logger.LogInformation($"Changelist {previousChangeNumber} -> " +
                                           $"{_currentChangeNumber} ({changedApps.Count} apps)");
                }


                var retryPolicy = Policy
                    .Handle<DbUpdateException>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, context) =>
                    {
                        _logger.LogWarning($"An error occurred while trying to save changes: {exception.Message}");
                    });


                //Batch operations in groups of 1000
                for (int i = 0; i < changedApps.Count; i += 1000)
                {
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        await using (var scope = _services.CreateAsyncScope())
                        {
                            using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                            var currentBatch = changedApps.Skip(i).Take(1000).ToList();
                            int newDepots = 0;

                            var appInfos = await _appInfoHandler.BulkLoadAppInfoAsync(currentBatch);

                            foreach (var depot in appInfos)
                            {
                                // Insert or update using Polly's retry policy
                                var dbDepot = await dbContext.SteamDepots.FirstOrDefaultAsync(d => d.SteamDepotId == depot.SteamDepotId && d.SteamAppId == depot.SteamAppId);
                                if (dbDepot == null)
                                {
                                    //Depot does not exist, create it
                                    dbDepot = new DbSteamDepot { SteamAppId = depot.SteamAppId, SteamDepotId = depot.SteamDepotId };
                                    dbContext.SteamDepots.Add(dbDepot);
                                    newDepots++;
                                }
                            }
                            //Save changes
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation($"Depots Processed: {i + currentBatch.Count}/{changedApps.Count}. Updated {currentBatch.Count - newDepots}, New {newDepots}");
                        }
                    });
                }

                await using (var scope = _services.CreateAsyncScope())
                {
                    using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();

                    var item = dbContext.Settings.FirstOrDefault(t => t.Key == DbSetting.SettingKey_SteamChangeNumber);
                    if (item == null)
                    {
                        item = new DbSetting { Key = DbSetting.SettingKey_SteamChangeNumber, Value = _currentChangeNumber.ToString() };
                        dbContext.Settings.Add(item);
                    }
                    else
                    {
                        item.Value = _currentChangeNumber.ToString();
                    }
                    await dbContext.SaveChangesAsync();
                }

                _logger.LogInformation("Waiting for 120 seconds before checking for new changes...");
                await Task.Delay(120_000, stoppingToken);
            }
        }

        //private HashSet<uint> PreviouslyProcessedAppIds()
        //{
        //    using var scope = _services.CreateAsyncScope();
        //    using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
        //    return dbContext.SteamApps.AsNoTracking().Select(e => e.AppId).ToHashSet();
        //}

        //TODO comment
        public async Task<List<App>> RetrieveAllAppIds2()
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

            _logger.LogInformation("Retrieved {appCount} apps", apiApps.Count);
            return apiApps;
        }
    }
}
