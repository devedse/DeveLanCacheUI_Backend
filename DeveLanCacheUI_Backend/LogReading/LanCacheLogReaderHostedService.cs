using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
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


            while (true)
            {
                var logLineSet = TailFrom(accessLogFilePath, stoppingToken).Take(1000).TakeWhile(t => t != null).ToList();

                await using (var scope = Services.CreateAsyncScope())
                {
                    using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();

                    var parsedLogLines = logLineSet.Select(LanCacheLogLineParser.ParseLogEntry);
                    var parsedLogLinesSteam = parsedLogLines.Where(t => t.Protocol == "steam");

                    Dictionary<int, DbSteamApp> steamAppsCache = new Dictionary<int, DbSteamApp>();
                    Dictionary<string, DbSteamAppDownloadEvent> steamAppDownloadEventsCache = new Dictionary<string, DbSteamAppDownloadEvent>();

                    var steamAppIds = parsedLogLinesSteam.GroupBy(t => t.SteamAppId);

                    foreach (var steamAppId in steamAppIds)
                    {
                        var foundInDb = await dbContext.SteamApps.FirstOrDefaultAsync(t => t.Id == steamAppId.Key);
                        if (foundInDb == null)
                        {
                            Console.WriteLine($"Found new Steam AppId: {steamAppId.Key.Value}");
                            foundInDb = new DbSteamApp()
                            {
                                Id = steamAppId.Key.Value,
                                AppName = "YeahDontKnowYet"
                            };
                            await dbContext.SteamApps.AddAsync(foundInDb);
                        }
                        else
                        {
                            var groupedOnClientIps = steamAppId.GroupBy(t => t.IpAddress);

                            foreach (var groupOnIp in groupedOnClientIps)
                            {
                                var firstUpdateEntryForThisAppId = steamAppId.OrderBy(t => t.DateTime).First();

                                //See if anything happened here in the last 5 minutes
                                var foundEventInCache = await dbContext.SteamAppDownloadEvents
                                    .FirstOrDefaultAsync(t =>
                                        t.SteamAppId == steamAppId.Key &&
                                        t.ClientIp == groupOnIp.Key &&
                                        t.LastUpdatedAt < firstUpdateEntryForThisAppId.DateTime.AddMinutes(-5)
                                        );

                                var cacheKey = $"{steamAppId.Key}_{groupOnIp.Key}";
                                if (foundEventInCache == null)
                                {
                                    foundEventInCache = new DbSteamAppDownloadEvent()
                                    {
                                        CreatedAt = firstUpdateEntryForThisAppId.DateTime,
                                        SteamAppId = steamAppId.Key.Value,
                                        ClientIp = groupOnIp.Key
                                    };
                                    await dbContext.SteamAppDownloadEvents.AddAsync(foundEventInCache);
                                }
                                steamAppDownloadEventsCache.Add(cacheKey, foundEventInCache);
                            }
                        }
                        steamAppsCache.Add(steamAppId.Key.Value, foundInDb);
                    }

                    foreach (var steamLogLine in parsedLogLinesSteam)
                    {
                        var cacheKey = $"{steamLogLine.SteamAppId}_{steamLogLine.IpAddress}";
                        var cachedEvent = steamAppDownloadEventsCache[cacheKey];

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
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);

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
