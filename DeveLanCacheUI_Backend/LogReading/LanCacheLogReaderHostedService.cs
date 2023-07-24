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
        public static Uri SkipLogLineReferrer = new Uri("http://develancacheui_skipthislogline");
        public static string SkipLogLineReferrerString = SkipLogLineReferrer.ToString();

        private readonly IServiceProvider _services;

        private readonly IConfiguration _configuration;
        private readonly IHubContext<LanCacheHub> _lanCacheHubContext;
        private readonly SteamManifestService _steamManifestService;
        private readonly ILogger<LanCacheLogReaderHostedService> _logger;

        public LanCacheLogReaderHostedService(IServiceProvider services,
            IConfiguration configuration,
            IHubContext<LanCacheHub> lanCacheHubContext,
            SteamManifestService steamManifestService,
            ILogger<LanCacheLogReaderHostedService> logger)
        {
            _services = services;
            _configuration = configuration;
            _lanCacheHubContext = lanCacheHubContext;
            _steamManifestService = steamManifestService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            await SimpleSteamDataSeeder.GoSeed(_services);

            await GoRun(stoppingToken);
        }

        private async Task GoRun(CancellationToken stoppingToken)
        {
            var oldestLog = DateTime.MinValue;
            await using (var scope = _services.CreateAsyncScope())
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

                await using (var scope = _services.CreateAsyncScope())
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
                        IEnumerable<LanCacheLogEntryRaw> filteredLogLines = currentSet;
                        filteredLogLines = filteredLogLines.Where(t => t.CacheIdentifier != "127.0.0.1");
                        filteredLogLines = filteredLogLines.Where(t => t.Referer != SkipLogLineReferrerString);

                        Dictionary<string, DbDownloadEvent> steamAppDownloadEventsCache = new Dictionary<string, DbDownloadEvent>();

                        foreach (var lanCacheLogLine in filteredLogLines)
                        {
                            if (lanCacheLogLine.CacheIdentifier == "steam" && lanCacheLogLine.Request.Contains("/manifest/") && DateTime.Now < lanCacheLogLine.DateTime.AddMinutes(5))
                            {
                                Console.WriteLine($"Found manifest for Depot: {lanCacheLogLine.DownloadIdentifier}");
                                var ttt = lanCacheLogLine;
                                _steamManifestService.TryToDownloadManifest(ttt);
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
