using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.LogReading.Models;
using DeveLanCacheUI_Backend.Steam;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.LogReading
{
    public class LanCacheLogReaderHostedService : BackgroundService
    {
        public IServiceProvider Services { get; }

        private readonly IConfiguration _configuration;
        private readonly ILogger<LanCacheLogReaderHostedService> _logger;

        public LanCacheLogReaderHostedService(IServiceProvider services,
            IConfiguration configuration,
            ILogger<LanCacheLogReaderHostedService> logger)
        {
            Services = services;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var oldestLog = DateTime.MinValue;
            await using (var scope = Services.CreateAsyncScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                var lastUpdatedItem = await dbContext.SteamAppDownloadEvents.OrderByDescending(t => t.LastUpdatedAt).FirstOrDefaultAsync();
                if (lastUpdatedItem != null)
                {
                    oldestLog = lastUpdatedItem.LastUpdatedAt;
                }
            }

            var logFilePath = _configuration.GetValue<string>("LogPath")!;
            if (logFilePath == null)
            {
                throw new NullReferenceException("LogPath == null, please ensure the LogPath ENVIRONMENT_VARIABLE is filled in");
            }
            var accessLogFilePath = Path.Combine(logFilePath, "access.log");


            var allLogLines = TailFrom(accessLogFilePath, stoppingToken);
            var parsedLogLines = allLogLines.Select(t => t == null ? null : LanCacheLogLineParser.ParseLogEntry(t));
            var batches = Batch2(parsedLogLines, 1000, oldestLog);

            int totalLinesProcessed = 0;

            foreach(var currentSet in batches)
            {
                //parsedLogLines = parsedLogLines
                //    .Where(t => t == null || t.DateTime > oldestLog)
                //    .Take(1000)
                //    .TakeWhile(t => t != null);
                Console.WriteLine($"Processing {currentSet.Count} lines... First DateTime: {currentSet.FirstOrDefault()?.DateTime} (Total processed: {totalLinesProcessed})");
                totalLinesProcessed += currentSet.Count;

                await using (var scope = Services.CreateAsyncScope())
                {
                    using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();

                    var parsedLogLinesSteam = currentSet.Where(t => t.Protocol == "steam");

                    Dictionary<int, DbSteamApp> steamAppsCache = new Dictionary<int, DbSteamApp>();
                    Dictionary<string, DbSteamAppDownloadEvent> steamAppDownloadEventsCache = new Dictionary<string, DbSteamAppDownloadEvent>();

                    var steamAppIds = parsedLogLinesSteam.GroupBy(t => t.SteamAppId);

                    foreach (var steamAppId in steamAppIds)
                    {
                        var foundInDb = await dbContext.SteamApps.FirstOrDefaultAsync(t => t.Id == steamAppId.Key);
                        if (foundInDb == null)
                        {
                            Console.WriteLine($"Found new Steam AppId: {steamAppId.Key.Value}");
                            var appId = steamAppId.Key.Value;
                            var foundApp = SteamApi.SteamApiData.applist.apps.Where(t => t.appid == appId).FirstOrDefault();
                            foundInDb = new DbSteamApp()
                            {
                                Id = appId,
                                AppName = foundApp?.name ?? "unknown"
                            };
                            await dbContext.SteamApps.AddAsync(foundInDb);
                        }


                        var groupedOnClientIps = steamAppId.GroupBy(t => t.IpAddress);

                        foreach (var groupOnIp in groupedOnClientIps)
                        {
                            var firstUpdateEntryForThisAppId = steamAppId.OrderBy(t => t.DateTime).First();

                            //See if anything happened here in the last 5 minutes
                            var foundEventInCache = await dbContext.SteamAppDownloadEvents
                                .FirstOrDefaultAsync(t =>
                                    t.SteamAppId == steamAppId.Key &&
                                    t.ClientIp == groupOnIp.Key &&
                                    t.LastUpdatedAt > firstUpdateEntryForThisAppId.DateTime.AddMinutes(-5)
                                    );

                            var cacheKey = $"{steamAppId.Key}_{groupOnIp.Key}";
                            if (foundEventInCache == null)
                            {
                                Console.WriteLine($"Adding new event: {cacheKey} ({firstUpdateEntryForThisAppId.DateTime})");
                                foundEventInCache = new DbSteamAppDownloadEvent()
                                {
                                    CreatedAt = firstUpdateEntryForThisAppId.DateTime,
                                    LastUpdatedAt = firstUpdateEntryForThisAppId.DateTime,
                                    SteamAppId = steamAppId.Key.Value,
                                    ClientIp = groupOnIp.Key
                                };
                                await dbContext.SteamAppDownloadEvents.AddAsync(foundEventInCache);
                            }
                            steamAppDownloadEventsCache.Add(cacheKey, foundEventInCache);
                        }
                        steamAppsCache.Add(steamAppId.Key.Value, foundInDb);
                    }

                    foreach (var steamLogLine in parsedLogLinesSteam)
                    {
                        //Console.WriteLine(steamLogLine.OriginalLogLine);

                        var cacheKey = $"{steamLogLine.SteamAppId}_{steamLogLine.IpAddress}";
                        var cachedEvent = steamAppDownloadEventsCache[cacheKey];

                        if (!(cachedEvent.LastUpdatedAt > steamLogLine.DateTime.AddMinutes(-5)))
                        {
                            Console.WriteLine($"Adding new event because more then 5 minutes no update: {cacheKey} ({cachedEvent.LastUpdatedAt} => {steamLogLine.DateTime})");
                            cachedEvent = new DbSteamAppDownloadEvent()
                            {
                                CreatedAt = steamLogLine.DateTime,
                                LastUpdatedAt = steamLogLine.DateTime,
                                SteamAppId = cachedEvent.SteamAppId,
                                ClientIp = cachedEvent.ClientIp
                            };
                            steamAppDownloadEventsCache[cacheKey] = cachedEvent;
                            await dbContext.SteamAppDownloadEvents.AddAsync(cachedEvent);
                        }

                        cachedEvent.LastUpdatedAt = steamLogLine.DateTime;
                        if (steamLogLine.CacheHitStatus == "HIT")
                        {
                            cachedEvent.CacheHitBytes += steamLogLine.ContentLength;
                        }
                        else
                        {
                            cachedEvent.CacheMissBytes += steamLogLine.ContentLength;
                        }
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
        }

        public IEnumerable<List<LanCacheLogEntry>> Batch2(IEnumerable<LanCacheLogEntry?> collection, int batchSize, DateTime skipOlderThen)
        {
            int skipCounter = 0;

            var nextbatch = new List<LanCacheLogEntry>();
            foreach (var logEntry in collection)
            {
                if (logEntry == null)
                {
                    if (nextbatch.Any())
                    {
                        yield return nextbatch;
                        nextbatch = new List<LanCacheLogEntry>();
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now} No new log lines, waiting...");
                        Thread.Sleep(1000);
                        continue;
                    }
                }
                else if (logEntry.DateTime > skipOlderThen)
                {
                    nextbatch.Add(logEntry);
                    if (nextbatch.Count == batchSize)
                    {
                        yield return nextbatch;
                        nextbatch = new List<LanCacheLogEntry>();
                    }
                }
                else
                {
                    skipCounter++;
                    if (skipCounter % 1000 == 0)
                    {
                        Console.WriteLine($"Skipped total of {skipCounter} lines (already processed)");
                    }
                }
            }
        }

        static IEnumerable<string> TailFrom(string file, CancellationToken stoppingToken)
        {
            using (var reader = File.OpenText(file))
            {
                // go to end - if the next line is commented out, all the lines from the beginning is returned
                // reader.BaseStream.Seek(0, SeekOrigin.End);
                while (true)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    string? line = reader.ReadLine();
                    if (reader.BaseStream.Length < reader.BaseStream.Position)
                    {
                        Console.WriteLine($"Uhh: {reader.BaseStream.Length} < {reader.BaseStream.Position}");
                        //reader.BaseStream.Seek(0, SeekOrigin.Begin);

                    }

                    if (line != null)
                    {
                        yield return line;
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
        }
    }
}
