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
            var logFilePath = _configuration.GetValue<string>("DepotFileCsvPath")!;

            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                logFilePath = Directory.GetCurrentDirectory();
            }

            Console.WriteLine($"Watching directory: '{logFilePath}' for any .CSV files to update our Depot database...");

            while (true)
            {
                var firstFile = Directory.GetFiles(logFilePath).Where(t => Path.GetExtension(t).Equals(".csv", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (firstFile != null)
                {
                    Console.WriteLine($"Found .CSV file to update our Depots Database: {firstFile}");

                    var depotToAppDict = new Dictionary<int, int>();

                    using (var reader = new StreamReader(firstFile))
                    {
                        string? line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            var values = line.Split(';');

                            if (values.Length < 3)
                            {
                                Console.WriteLine("Warning: Line does not contain sufficient data, skipping");
                                continue;
                            }

                            bool appIdParsed = int.TryParse(values[0], out int appId);
                            bool depotIdParsed = int.TryParse(values[2], out int depotId);

                            if (!appIdParsed || !depotIdParsed)
                            {
                                Console.WriteLine("Warning: AppId or DepotId could not be parsed, skipping");
                                continue;
                            }

                            if (!depotToAppDict.ContainsKey(depotId))
                                depotToAppDict.Add(depotId, appId);
                            else
                                Console.WriteLine("Warning: Duplicate depotId found, skipping");
                        }
                    }

                    Console.WriteLine($"Depot File {firstFile} read. Adding {depotToAppDict.Count} entries to db...");

                    await using (var scope = Services.CreateAsyncScope())
                    {
                      


                        var retryPolicy = Policy
                            .Handle<DbUpdateException>()
                            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                            (exception, timeSpan, context) =>
                            {
                                Console.WriteLine($"An error occurred while trying to save changes: {exception.Message}");
                            });

                        var depotList = depotToAppDict.Keys.ToList();

                        //Batch operations in groups of 1000
                        for (int i = 0; i < depotList.Count; i += 1000)
                        {
                            await retryPolicy.ExecuteAsync(async () =>
                            {
                                using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();


                                var currentBatch = depotList.Skip(i).Take(1000).ToList();

                                foreach (var depotId in currentBatch)
                                {
                                    // Insert or update using Polly's retry policy

                                    var depot = await dbContext.SteamDepots.FirstOrDefaultAsync(d => d.Id == depotId);

                                    if (depot == null)
                                    {
                                        //Depot does not exist, create it
                                        depot = new DbSteamDepot { Id = depotId };
                                        dbContext.SteamDepots.Add(depot);
                                    }

                                    //Link the depot to the existing app
                                    depot.SteamAppId = depotToAppDict[depotId];

                                    //Save changes
                                    await dbContext.SaveChangesAsync();

                                }
                                Console.WriteLine($"Updated {currentBatch.Count} depots");
                            });
                        }


                    }



                    var processedDirectoryPath = Path.Combine(logFilePath, "processed");
                    Directory.CreateDirectory(processedDirectoryPath);
                    var newFileName = Path.GetFileNameWithoutExtension(logFilePath) + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + Path.GetExtension(firstFile);
                    var newFilePath = Path.Combine(processedDirectoryPath, newFileName);
                    File.Move(firstFile, newFilePath);
                    Console.WriteLine($"File {firstFile} moved to {newFilePath}");
                }
            }

        }
    }
}
