namespace DeveLanCacheUI_Backend.Services
{
    //TODO document what this is for
    public class SteamAppInfoService : BackgroundService
    {
        private readonly ILogger<SteamAppInfoService> _logger;
        private readonly Steam3Session _steam3Session;
        private readonly AppInfoHandler _appInfoHandler;
        private readonly IServiceProvider _services;

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

            var picsChangesResult = await _steam3Session.SteamAppsApi.PICSGetChangesSince().ToTask();
            var currentChangeNumber = picsChangesResult.CurrentChangeNumber;
            _logger.LogInformation("Current changeset : {changesetId}", currentChangeNumber);

            await ProcessAllKnownAppIds();

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            await WatchForAppUpdates(stoppingToken);
        }

        /* TODO handle the "task was cancelled" and "key not found in dictionary" exceptions more gracefully.
            They don't actually hurt anything, as this code will automatically recover, but they do look concerning in the logs
         */
        private async Task ProcessAllKnownAppIds()
        {
            // Get all currently known (or unknown) app ids from Steam
            List<uint> allKnownAppIds = await RetrieveAllAppIds2();

            // Filter out any app ids that have been previously processed
            //var previouslyProcessedIds = PreviouslyProcessedAppIds();
            //var appIdsToProcess = allKnownAppIds.Where(e => !previouslyProcessedIds.Contains(e))
            //                                    .OrderBy(e => e)
            //                                    .ToList();
            var appIdsToProcess = allKnownAppIds;

            var chunkSize = 1000;
            var batches = appIdsToProcess.Chunk(chunkSize).ToList();
            
            _logger.LogInformation("Retrieving app metadata...");
            while (batches.Any())
            {
                try
                {
                    _logger.LogInformation("{remainingCount} apps remaining", batches.Sum(e => e.Length).ToString("N0"));
                    var currentBatch = batches.First().ToList();

                    // Retrieving app info
                    var appInfos = await _appInfoHandler.RetrieveAppMetadataAsync(currentBatch);

                    //// Persisting to DB
                    //using var scope = _services.CreateAsyncScope();
                    //using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                    //var models = appInfos.Select(e => new DbSteamAppInfo
                    //{
                    //    AppId = e.AppId,
                    //    Name = e.Name ?? "",
                    //    Depots = e.Depots.Select(e => new DbSteamDepot
                    //    {
                    //        Id = e.DepotId
                    //    }).ToList()
                    //});
                    //dbContext.AddRange(models);
                    //dbContext.SaveChanges();

                    // If successful pop it from the list of jobs
                    batches.RemoveAt(0);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error while retrieving app info");
                }
            }

            _appInfoHandler.ClearCachedAppInfos();
            _logger.LogInformation("Done loading app metadata");
        }

        private async Task WatchForAppUpdates(CancellationToken stoppingToken)
        {
            uint previousChangeNumber = 0;
            _logger.LogInformation("MWaiting a minute before requesting latest app changes....");
            await Task.Delay(60_000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await _steam3Session.SteamAppsApi.PICSGetChangesSince(previousChangeNumber).ToTask();
                if (result.AppChanges.Any())
                {
                    _logger.LogInformation($"Changelist {previousChangeNumber} -> " +
                                           $"{result.CurrentChangeNumber} ({result.AppChanges.Count} apps, {result.PackageChanges.Count} packages)");
                }

                //TODO implement diff logic here

                previousChangeNumber = result.CurrentChangeNumber;
                _appInfoHandler.ClearCachedAppInfos();

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
        public async Task<List<uint>> RetrieveAllAppIds2()
        {
            _logger.LogInformation("Retrieving all known AppIds");

            using var steamAppsApi = _steam3Session.Configuration.GetAsyncWebAPIInterface("ISteamApps");
            var response = await steamAppsApi.CallAsync(HttpMethod.Get, "GetAppList", 2);

            var apiApps = response["apps"].Children.Select(app => app["appid"].AsUnsignedInteger()).ToList();

            _logger.LogInformation("Retrieved {appCount} apps", apiApps.Count);
            return apiApps;
        }
    }
}
