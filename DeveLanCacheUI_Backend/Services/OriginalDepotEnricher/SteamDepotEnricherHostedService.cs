namespace DeveLanCacheUI_Backend.Services.OriginalDepotEnricher
{
    public class SteamDepotEnricherHostedService : BackgroundService
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            await GoRun(stoppingToken);
        }


        private async Task GoRun(CancellationToken stoppingToken)
        {
            var deveLanCacheUIDataDirectory = _deveLanCacheConfiguration.DeveLanCacheUIDataDirectory;
            if (string.IsNullOrWhiteSpace(deveLanCacheUIDataDirectory))
            {
                deveLanCacheUIDataDirectory = Directory.GetCurrentDirectory();
            }

            var depotFileDirectory = Path.Combine(deveLanCacheUIDataDirectory, "depotdir");

            _logger.LogInformation($"Watching directory: '{depotFileDirectory}' for any .CSV files to update our Depot database...");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (Directory.Exists(depotFileDirectory))
                {
                    var firstFile = Directory.GetFiles(depotFileDirectory).Where(t => Path.GetExtension(t).Equals(".csv", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (firstFile != null)
                    {
                        Console.WriteLine($"Found .CSV file to update our Depots Database: {firstFile}");


                        var curFileSize = new FileInfo(firstFile).Length;
                        Console.WriteLine($"Waiting for file size to not increase anymore (as in, the copy is done). Current Size: {curFileSize}");

                        var fileSizeTimer = Stopwatch.StartNew();

                        while (fileSizeTimer.Elapsed.TotalSeconds < 5)
                        {
                            var newFileSize = new FileInfo(firstFile).Length;
                            if (curFileSize != newFileSize)
                            {
                                _logger.LogInformation($"File size has increased, waiting 5 more seconds... from: {curFileSize} to {newFileSize}");
                                curFileSize = newFileSize;
                                fileSizeTimer.Restart();
                            }
                            else
                            {
                                _logger.LogInformation($"File size equal for {fileSizeTimer.Elapsed}");
                            }
                            await Task.Delay(1000);
                        }

                        var desiredSteamAppToDepots = new List<SteamDepotEnricherCSVModel>();
                        //var depotToAppDict = new Dictionary<uint, uint>();

                        try
                        {
                            using (var reader = new StreamReader(firstFile))
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

                            Console.WriteLine($"Depot File {firstFile} read. Adding {desiredSteamAppToDepots.Count} entries to db...");


                            desiredSteamAppToDepots = desiredSteamAppToDepots.DistinctBy(t => new { t.SteamAppId, t.SteamDepotId }).ToList();

                            var retryPolicy = Policy
                                .Handle<DbUpdateException>()
                                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                (exception, timeSpan, context) =>
                                {
                                    _logger.LogWarning($"An error occurred while trying to save changes: {exception.Message}");
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

                            var processedDirectoryPath = Path.Combine(depotFileDirectory, "processed");
                            Directory.CreateDirectory(processedDirectoryPath);
                            var newFileName = Path.GetFileNameWithoutExtension(firstFile) + "_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + Path.GetExtension(firstFile);
                            var newFilePath = Path.Combine(processedDirectoryPath, newFileName);
                            File.Move(firstFile, newFilePath);
                            Console.WriteLine($"File {firstFile} moved to {newFilePath}");
                        }
                        catch (IOException ex)
                        {
                            _logger.LogWarning($"IO Exception while reading/writing file. This could be because file is in use. Retrying...");
                        }
                    }
                }
                await Task.Delay(1000);
            }

        }
    }
}
