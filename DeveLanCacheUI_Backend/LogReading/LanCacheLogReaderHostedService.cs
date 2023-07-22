using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.Hubs;
using DeveLanCacheUI_Backend.LogReading.Models;
using DeveLanCacheUI_Backend.Steam;
using DeveLanCacheUI_Backend.SteamProto;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Text;

namespace DeveLanCacheUI_Backend.LogReading
{
    public class LanCacheLogReaderHostedService : BackgroundService
    {
        public IServiceProvider Services { get; }

        private readonly IConfiguration _configuration;
        private readonly IHubContext<LanCacheHub> _lanCacheHubContext;
        private readonly IHttpClientFactory _httpClientFactoryForManifestDownloads;
        private readonly ILogger<LanCacheLogReaderHostedService> _logger;

        private const bool StoreSteamDbProtoManifestBytesInDb = true;

        public LanCacheLogReaderHostedService(IServiceProvider services,
            IConfiguration configuration,
            IHubContext<LanCacheHub> lanCacheHubContext,
            IHttpClientFactory httpClientFactory,
            ILogger<LanCacheLogReaderHostedService> logger)
        {
            Services = services;
            _configuration = configuration;
            _lanCacheHubContext = lanCacheHubContext;
            _httpClientFactoryForManifestDownloads = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            await SimpleSteamDataSeeder.GoSeed(Services);

            await GoRun(stoppingToken);
        }

        private async Task GoRun(CancellationToken stoppingToken)
        {
            var oldestLog = DateTime.MinValue;
            await using (var scope = Services.CreateAsyncScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                var lastUpdatedItem = await dbContext.DownloadEvents.OrderByDescending(t => t.LastUpdatedAt).FirstOrDefaultAsync();
                if (lastUpdatedItem != null)
                {
                    oldestLog = lastUpdatedItem.LastUpdatedAt;
                }
            }

            var logFilePath = _configuration.GetValue<string>("LanCacheLogsDirectory")!;
            if (logFilePath == null)
            {
                throw new NullReferenceException("LanCacheLogsDirectory == null, please ensure the LanCacheLogsDirectory ENVIRONMENT_VARIABLE is filled in");
            }
            var accessLogFilePath = Path.Combine(logFilePath, "access.log");


            var allLogLines = TailFrom2(accessLogFilePath, stoppingToken);
            var parsedLogLines = allLogLines.Select(t => t == null ? null : LanCacheLogLineParser.ParseLogEntry(t));
            var batches = Batch2(parsedLogLines, 1000, oldestLog);

            int totalLinesProcessed = 0;

            foreach (var currentSet in batches)
            {
                //parsedLogLines = parsedLogLines
                //    .Where(t => t == null || t.DateTime > oldestLog)
                //    .Take(1000)
                //    .TakeWhile(t => t != null);
                Console.WriteLine($"Processing {currentSet.Count} lines... First DateTime: {currentSet.FirstOrDefault()?.DateTime} (Total processed: {totalLinesProcessed})");
                totalLinesProcessed += currentSet.Count;

                await using (var scope = Services.CreateAsyncScope())
                {


                    var retryPolicy = Policy
                        .Handle<DbUpdateException>()
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        (exception, timeSpan, context) =>
                        {
                            Console.WriteLine($"An error occurred while trying to save changes: {exception.Message}");
                        });

                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();

                        //var filteredLogLines = currentSet.Where(t => t.CacheIdentifier == "steam");
                        var filteredLogLines = currentSet.Where(t => t.CacheIdentifier != "127.0.0.1");

                        //Dictionary<int, DbSteamDepot> steamDepotsCache = new Dictionary<int, DbSteamDepot>();
                        Dictionary<string, DbDownloadEvent> steamAppDownloadEventsCache = new Dictionary<string, DbDownloadEvent>();

                        //var groupedLogLines = parsedLogLinesSteam.GroupBy(t => new { t.CacheIdentifier, t.DownloadIdentifier });

                        //foreach (var group in groupedLogLines)
                        //{
                        //    var groupedOnClientIps = group.GroupBy(t => t.RemoteAddress);

                        //    foreach (var groupOnIp in groupedOnClientIps)
                        //    {
                        //        var firstUpdateEntryForThisDepotId = group.OrderBy(t => t.DateTime).First();

                        //        //See if anything happened here in the last 5 minutes
                        //        var foundEventInCache = await dbContext.DownloadEvents
                        //            .FirstOrDefaultAsync(t =>
                        //                t.CacheIdentifier == group.Key.CacheIdentifier &&
                        //                t.DownloadIdentifierString == group.Key.DownloadIdentifier &&
                        //                t.ClientIp == groupOnIp.Key &&
                        //                t.LastUpdatedAt > firstUpdateEntryForThisDepotId.DateTime.AddMinutes(-5)
                        //                );

                        //        if (foundEventInCache != null)
                        //        {
                        //            var cacheKey = $"{group.Key.CacheIdentifier}_||_{group.Key.DownloadIdentifier}_||_{groupOnIp.Key}";
                        //            steamAppDownloadEventsCache.Add(cacheKey, foundEventInCache);
                        //        }

                        //        //var cacheKey = $"{group.Key.CacheIdentifier}_||_{group.Key.DownloadIdentifier}_||_{groupOnIp.Key}";
                        //        //if (foundEventInCache == null)
                        //        //{
                        //        //    Console.WriteLine($"Adding new event: {cacheKey} ({firstUpdateEntryForThisDepotId.DateTime})");
                        //        //    int.TryParse(group.Key.DownloadIdentifier, out var downloadIdentifierInt);
                        //        //    foundEventInCache = new DbDownloadEvent()
                        //        //    {
                        //        //        CacheIdentifier = group.Key.CacheIdentifier,
                        //        //        DownloadIdentifier = downloadIdentifierInt,
                        //        //        DownloadIdentifierString = group.Key.DownloadIdentifier,
                        //        //        CreatedAt = firstUpdateEntryForThisDepotId.DateTime,
                        //        //        LastUpdatedAt = firstUpdateEntryForThisDepotId.DateTime,
                        //        //        ClientIp = groupOnIp.Key
                        //        //    };
                        //        //    await dbContext.DownloadEvents.AddAsync(foundEventInCache);
                        //        //}
                        //        //steamAppDownloadEventsCache.Add(cacheKey, foundEventInCache);
                        //    }
                        //}

                        foreach (var lanCacheLogLine in filteredLogLines)
                        {
                            if (lanCacheLogLine.CacheIdentifier == "steam" && lanCacheLogLine.Request.Contains("/manifest/") && DateTime.Now < lanCacheLogLine.DateTime.AddMinutes(5))
                            {
                                Console.WriteLine($"Found manifest for Depot: {lanCacheLogLine.CacheIdentifier}");
                                var ttt = lanCacheLogLine;
                                TryToDownloadManifest(ttt);
                            }

                            var cacheKey = $"{lanCacheLogLine.CacheIdentifier}_||_{lanCacheLogLine.DownloadIdentifier}_||_{lanCacheLogLine.RemoteAddress}";
                            steamAppDownloadEventsCache.TryGetValue(cacheKey, out var cachedEvent);

                            if (cachedEvent == null)
                            {
                                cachedEvent = await dbContext.DownloadEvents
                                   .FirstOrDefaultAsync(t =>
                                       t.CacheIdentifier == lanCacheLogLine.CacheIdentifier &&
                                       t.DownloadIdentifierString == lanCacheLogLine.DownloadIdentifier &&
                                       t.ClientIp == lanCacheLogLine.RemoteAddress &&
                                       t.LastUpdatedAt > lanCacheLogLine.DateTime.AddMinutes(-5)
                                       );
                                if (cachedEvent != null)
                                {
                                    steamAppDownloadEventsCache[cacheKey] = cachedEvent;
                                }
                            }

                            if (cachedEvent == null || !(cachedEvent.LastUpdatedAt > lanCacheLogLine.DateTime.AddMinutes(-5)))
                            {
                                Console.WriteLine($"Adding new event because more then 5 minutes no update: {cacheKey} ({lanCacheLogLine.DateTime})");

                                int.TryParse(lanCacheLogLine.DownloadIdentifier, out var downloadIdentifierInt);
                                cachedEvent = new DbDownloadEvent()
                                {
                                    CacheIdentifier = lanCacheLogLine.CacheIdentifier,
                                    DownloadIdentifierString = lanCacheLogLine.DownloadIdentifier,
                                    DownloadIdentifier = downloadIdentifierInt,
                                    CreatedAt = lanCacheLogLine.DateTime,
                                    LastUpdatedAt = lanCacheLogLine.DateTime,
                                    ClientIp = lanCacheLogLine.RemoteAddress
                                };
                                steamAppDownloadEventsCache[cacheKey] = cachedEvent;
                                await dbContext.DownloadEvents.AddAsync(cachedEvent);
                            }

                            cachedEvent.LastUpdatedAt = lanCacheLogLine.DateTime;
                            if (lanCacheLogLine.UpstreamCacheStatus == "HIT")
                            {
                                cachedEvent.CacheHitBytes += lanCacheLogLine.BodyBytesSentLong;
                            }
                            else
                            {
                                cachedEvent.CacheMissBytes += lanCacheLogLine.BodyBytesSentLong;
                            }
                        }

                        await dbContext.SaveChangesAsync();
                        await _lanCacheHubContext.Clients.All.SendAsync("UpdateDownloadEvents");
                    });
                }
            }
        }

        private void TryToDownloadManifest(LanCacheLogEntryRaw lanCacheLogEntryRaw)
        {
            _ = Task.Run(async () =>
            {
                var fallbackPolicy = Policy
                    .Handle<Exception>()
                    .FallbackAsync(async (ct) =>
                    {
                        Console.WriteLine($"Manifest saving: All retries failed, skipping...");
                    });

                var retryPolicy = Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                   (exception, timeSpan, context) =>
                   {
                       Console.WriteLine($"Manifest saving: An error occurred while trying to save changes: {exception.Message}");
                   });

                await fallbackPolicy.WrapAsync(retryPolicy).ExecuteAsync(async () =>
                {
                    await using (var scope = Services.CreateAsyncScope())
                    {
                        using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                        using var httpClient = _httpClientFactoryForManifestDownloads.CreateClient();
                        var theManifestUrlPart = lanCacheLogEntryRaw.Request.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
                        var url = $"http://{lanCacheLogEntryRaw.Host}{theManifestUrlPart}";
                        var manifestResponse = await httpClient.GetAsync(url);
                        if (!manifestResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Waring: Tried to obtain manifest for: {lanCacheLogEntryRaw.CacheIdentifier} but status code was: {manifestResponse.StatusCode}");
                        }
                        var manifestBytes = await manifestResponse.Content.ReadAsByteArrayAsync();
                        var dbManifest = SteamManifestHelper.ManifestBytesToDbSteamManifest(manifestBytes, StoreSteamDbProtoManifestBytesInDb);

                        if (dbManifest == null)
                        {
                            Console.WriteLine($"Waring: Could not get manifest for depot: {lanCacheLogEntryRaw.CacheIdentifier}");
                        }

                        var dbValue = dbContext.SteamManifests.FirstOrDefault(t => t.DepotId == dbManifest.DepotId && t.CreationTime == dbManifest.CreationTime);
                        if (dbValue != null)
                        {
                            dbContext.Entry(dbValue).CurrentValues.SetValues(dbManifest);
                            Console.WriteLine($"Info: Updated manifest for {lanCacheLogEntryRaw.CacheIdentifier}");
                        }
                        else
                        {
                            await dbContext.SteamManifests.AddAsync(dbManifest);
                            Console.WriteLine($"Info: Added manifest for {lanCacheLogEntryRaw.CacheIdentifier}");
                        }
                        await dbContext.SaveChangesAsync();
                    }
                });
            });
        }

        public IEnumerable<List<LanCacheLogEntryRaw>> Batch2(IEnumerable<LanCacheLogEntryRaw?> collection, int batchSize, DateTime skipOlderThen)
        {
            int skipCounter = 0;

            int dontLogForSpecificCounter = 0;

            var nextbatch = new List<LanCacheLogEntryRaw>();
            foreach (var logEntry in collection)
            {
                if (logEntry == null)
                {
                    if (nextbatch.Any())
                    {
                        yield return nextbatch;
                        nextbatch = new List<LanCacheLogEntryRaw>();
                        dontLogForSpecificCounter = 0;
                    }
                    else
                    {
                        //Only log once in 30 times
                        if (dontLogForSpecificCounter % 30 == 0)
                        {
                            Console.WriteLine($"{DateTime.Now} No new log lines, waiting...");
                        }
                        dontLogForSpecificCounter++;
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
                        nextbatch = new List<LanCacheLogEntryRaw>();
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


        static IEnumerable<string> TailFrom2(string file, CancellationToken stoppingToken)
        {

            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                const int BufferSize = 1024;
                var buffer = new byte[BufferSize];
                var leftoverBuffer = new List<byte>();
                int bytesRead;

                while (true)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    bytesRead = fileStream.Read(buffer, 0, BufferSize);

                    if (bytesRead == 0)
                    {
                        yield return null;
                        continue;
                    }

                    int newlineIndex;
                    var searchStartIndex = 0;

                    while ((newlineIndex = Array.IndexOf(buffer, (byte)'\n', searchStartIndex, bytesRead - searchStartIndex)) != -1)
                    {
                        // Include \r in the line if present
                        var lineEndIndex = newlineIndex > 0 && buffer[newlineIndex - 1] == '\r' ? newlineIndex - 1 : newlineIndex;

                        var lineBuffer = new byte[leftoverBuffer.Count + lineEndIndex - searchStartIndex];
                        leftoverBuffer.CopyTo(lineBuffer);
                        Array.Copy(buffer, searchStartIndex, lineBuffer, leftoverBuffer.Count, lineEndIndex - searchStartIndex);

                        yield return Encoding.UTF8.GetString(lineBuffer);

                        leftoverBuffer.Clear();
                        searchStartIndex = newlineIndex + 1;
                    }

                    // Save leftover data for next loop
                    leftoverBuffer.AddRange(buffer.Skip(searchStartIndex).Take(bytesRead - searchStartIndex));
                }
            }
        }
    }
}
