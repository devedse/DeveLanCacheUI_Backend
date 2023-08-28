namespace DeveLanCacheUI_Backend.Services
{
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

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            await WatchForAppUpdates(stoppingToken);
        }

        private async Task WatchForAppUpdates(CancellationToken stoppingToken)
        {
            await _appInfoHandler.EnsureAppsAreLoaded();

            _logger.LogInformation("Loading latest app changes....");

            while (!stoppingToken.IsCancellationRequested)
            {
                uint previousChangeNumber = _currentChangeNumber;

                List<uint> changedApps = new List<uint>();
                if (_currentChangeNumber == 0)
                {
                    var picsChangesResult = await _steam3Session.SteamAppsApi.PICSGetChangesSince().ToTask();
                    var currentChangeNumber = picsChangesResult.CurrentChangeNumber;
                    _currentChangeNumber = currentChangeNumber;
                    changedApps = (await _appInfoHandler.RetrieveAllAppIds2()).Select(t => t.appid).ToList();
                }
                else
                {
                    var result = await _steam3Session.SteamAppsApi.PICSGetChangesSince(previousChangeNumber).ToTask();
                    _currentChangeNumber = result.CurrentChangeNumber;
                    changedApps = result.AppChanges.Select(e => e.Value.ID).ToList();

                    if (changedApps.Count == 0 && previousChangeNumber - _currentChangeNumber >= 1000)
                    {
                        _logger.LogWarning($"No changes obtained from Steam for changelist {previousChangeNumber} -> {_currentChangeNumber}. This is usually because the changeSet we had was too old. Falling back to re-obtain all apps.");
                        _currentChangeNumber = 0;
                        continue;
                    }
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

                int totalDepotsProcessed = 0;

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
                                if (!string.IsNullOrWhiteSpace(depot.SteamAppName))
                                {
                                    _logger.LogTrace("Updating depot mapping for app " + depot.SteamAppName + " with id " + depot.SteamAppId + " and depot " + depot.SteamDepotId);
                                }

                                totalDepotsProcessed++;

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
                            _logger.LogInformation($"Depots Processed: {totalDepotsProcessed}/???? Apps Processed: {i + currentBatch.Count}/{changedApps.Count}. Updated {appInfos.Count - newDepots}, New {newDepots}");
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

                throw new TaskCanceledException();
            }
        }
    }
}
