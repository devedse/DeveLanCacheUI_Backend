namespace DeveLanCacheUI_Backend.Services.OriginalDepotEnricher
{
    public class SteamDepotEnricherHostedService
    {
        public IServiceProvider Services { get; }

        private readonly DeveLanCacheConfiguration _deveLanCacheConfiguration;
        private readonly ILogger<SteamDepotEnricherHostedService> _logger;

        public SteamDepotEnricherHostedService(IServiceProvider services,
            DeveLanCacheConfiguration deveLanCacheConfiguration,
            ILogger<SteamDepotEnricherHostedService> logger)
        {
            Services = services;
            _deveLanCacheConfiguration = deveLanCacheConfiguration;
            _logger = logger;
        }

        public async Task GoProcess(Version depotFileVersion, byte[] downloadedDepotFile, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Processing depot file with version {DepotFileVersion}", depotFileVersion);

            var desiredSteamAppToDepots = new List<SteamDepotEnricherCSVModel>();
            //var depotToAppDict = new Dictionary<uint, uint>();

            using (var reader = new StreamReader(new MemoryStream(downloadedDepotFile)))
            {
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    var values = line.Split(';');

                    if (values.Length < 3)
                    {
                        //Console.WriteLine("Warning: Line does not contain sufficient data, skipping");
                        continue;
                    }

                    bool appIdParsed = uint.TryParse(values[0], out uint appId);
                    var appName = values[1];
                    bool depotIdParsed = uint.TryParse(values[2], out uint depotId);

                    if (!appIdParsed || !depotIdParsed)
                    {
                        //Console.WriteLine("Warning: AppId or DepotId could not be parsed, skipping");
                        continue;
                    }

                    //create csv model
                    var csvModel = new SteamDepotEnricherCSVModel
                    {
                        SteamAppId = appId,
                        SteamAppName = appName,
                        SteamDepotId = depotId
                    };

                    desiredSteamAppToDepots.Add(csvModel);
                }
            }

            _logger.LogInformation("Depot data read. Adding {DepotCount} entries to db...", desiredSteamAppToDepots.Count);

            desiredSteamAppToDepots = desiredSteamAppToDepots.DistinctBy(t => new { t.SteamAppId, t.SteamDepotId }).ToList();

            var retryPolicy = Policy
                .Handle<DbUpdateException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, context) =>
                {
                    _logger.LogWarning("An error occurred while trying to save changes: {Message}", exception.Message);
                });

            //Batch operations in groups of 1000
            for (int i = 0; i < desiredSteamAppToDepots.Count; i += 1000)
            {
                await retryPolicy.ExecuteAsync(async () =>
                {
                    await using (var scope = Services.CreateAsyncScope())
                    {
                        using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                        var currentBatch = desiredSteamAppToDepots.Skip(i).Take(1000).ToList();
                        int newDepots = 0;
                        foreach (var depot in currentBatch)
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
                        _logger.LogInformation($"Depots Processed: {i + currentBatch.Count}/{desiredSteamAppToDepots.Count}. Updated {currentBatch.Count - newDepots}, New {newDepots}");
                    }
                });
            }

            _logger.LogInformation("Depot data processing completed. Total entries added: {TotalEntries}", desiredSteamAppToDepots.Count);

            await using (var scope = Services.CreateAsyncScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                var foundSetting = await dbContext.Settings.FirstOrDefaultAsync(t => t.Key == DbSetting.SettingKey_DepotVersion);
                if (foundSetting == null || foundSetting.Value == null)
                {
                    foundSetting = new DbSetting()
                    {
                        Key = DbSetting.SettingKey_DepotVersion,
                        Value = depotFileVersion.ToString()
                    };
                    dbContext.Settings.Add(foundSetting);
                }
                else
                {
                    foundSetting.Value = depotFileVersion.ToString();
                }
                await dbContext.SaveChangesAsync();
            }

            _logger.LogInformation("Depot file processing completed successfully. Version {DepotFileVersion} saved to database.", depotFileVersion);
        }
    }
}
