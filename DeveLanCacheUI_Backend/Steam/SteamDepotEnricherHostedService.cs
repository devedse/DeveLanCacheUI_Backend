using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.LogReading.Models;
using DeveLanCacheUI_Backend.Steam;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace DeveLanCacheUI_Backend.LogReading
{
    public class SteamDepotEnricherHostedService : BackgroundService
    {
        public IServiceProvider Services { get; }

        private readonly IConfiguration _configuration;
        private readonly ILogger<SteamDepotEnricherHostedService> _logger;

        public SteamDepotEnricherHostedService(IServiceProvider services,
            IConfiguration configuration,
            ILogger<SteamDepotEnricherHostedService> logger)
        {
            Services = services;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            await GoRun(stoppingToken);
        }

        private async Task GoRun(CancellationToken stoppingToken)
        {
            var depotFileDirectory = _configuration.GetValue<string>("DepotFileDirectory")!;

            if (string.IsNullOrWhiteSpace(depotFileDirectory))
            {
                depotFileDirectory = Directory.GetCurrentDirectory();
            }

            _logger.LogInformation($"Watching directory: '{depotFileDirectory}' for any .CSV files to update our Depot database...");

            while (!stoppingToken.IsCancellationRequested)
            {
                var firstFile = Directory.GetFiles(depotFileDirectory).Where(t => Path.GetExtension(t).Equals(".csv", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (firstFile != null)
                {
                    Console.WriteLine($"Found .CSV file to update our Depots Database: {firstFile}");

                    var depotToAppDict = new Dictionary<int, int>();

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

                                bool appIdParsed = int.TryParse(values[0], out int appId);
                                bool depotIdParsed = int.TryParse(values[2], out int depotId);

                                if (!appIdParsed || !depotIdParsed)
                                {
                                    //Console.WriteLine("Warning: AppId or DepotId could not be parsed, skipping");
                                    continue;
                                }

                                if (!depotToAppDict.ContainsKey(depotId))
                                {
                                    depotToAppDict.Add(depotId, appId);
                                }
                                else
                                {
                                    //Console.WriteLine("Warning: Duplicate depotId found, skipping");
                                }
                            }
                        }

                        Console.WriteLine($"Depot File {firstFile} read. Adding {depotToAppDict.Count} entries to db...");





                        var retryPolicy = Policy
                            .Handle<DbUpdateException>()
                            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                            (exception, timeSpan, context) =>
                            {
                                _logger.LogWarning($"An error occurred while trying to save changes: {exception.Message}");
                            });

                        var depotList = depotToAppDict.Keys.ToList();

                        //Batch operations in groups of 1000
                        for (int i = 0; i < depotList.Count; i += 1000)
                        {
                            await retryPolicy.ExecuteAsync(async () =>
                            {
                                await using (var scope = Services.CreateAsyncScope())
                                {
                                    using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();


                                    var currentBatch = depotList.Skip(i).Take(1000).ToList();

                                    int newDepots = 0;

                                    foreach (var depotId in currentBatch)
                                    {
                                        // Insert or update using Polly's retry policy

                                        var depot = await dbContext.SteamDepots.FirstOrDefaultAsync(d => d.Id == depotId);

                                        if (depot == null)
                                        {
                                            //Depot does not exist, create it
                                            depot = new DbSteamDepot { Id = depotId };
                                            dbContext.SteamDepots.Add(depot);
                                            newDepots++;
                                        }

                                        //Link the depot to the existing app
                                        depot.SteamAppId = depotToAppDict[depotId];


                                    }
                                    //Save changes
                                    await dbContext.SaveChangesAsync();

                                    _logger.LogInformation($"Depots Processed: {i}/{depotList.Count}. Updated {currentBatch.Count - newDepots}, New {newDepots}");
                                }
                            });
                        }






                        var processedDirectoryPath = Path.Combine(depotFileDirectory, "processed");
                        Directory.CreateDirectory(processedDirectoryPath);
                        var newFileName = Path.GetFileNameWithoutExtension(depotFileDirectory) + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + Path.GetExtension(firstFile);
                        var newFilePath = Path.Combine(processedDirectoryPath, newFileName);
                        File.Move(firstFile, newFilePath);
                        Console.WriteLine($"File {firstFile} moved to {newFilePath}");
                    }
                    catch (System.IO.IOException ex)
                    {
                        _logger.LogWarning($"IO Exception while reading/writing file. This could be because file is in use. Retrying...");
                    }
                }
            }

        }
    }
}
