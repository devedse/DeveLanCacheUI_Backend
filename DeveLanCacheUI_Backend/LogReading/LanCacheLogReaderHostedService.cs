using DbContext = DeveLanCacheUI_Backend.Db.DeveLanCacheUIDbContext;

namespace DeveLanCacheUI_Backend.LogReading
{
    public class LanCacheLogReaderHostedService : BackgroundService
    {
        public static Uri SkipLogLineReferrer = new Uri("http://develancacheui_skipthislogline");
        public static string SkipLogLineReferrerString = SkipLogLineReferrer.ToString();

        private readonly IServiceProvider _services;
        private readonly DeveLanCacheConfiguration _deveLanCacheConfiguration;
        private readonly ILogger<LanCacheLogReaderHostedService> _logger;

        /// <summary>
        /// These app ids will be excluded from processing as they introduce a lot of noise in the logs.
        /// These are primarily the "Direct X Runtime" and ".NET Runtime" installers that are shared by nearly
        /// every single game on Steam.  Since they're so small and so frequent, excluding them should make the
        /// remaining logs far more usable.
        /// </summary>
        private readonly HashSet<string> ExcludedAppIds = new HashSet<string>()
        {
            //"229033",
            //"229000",
            //"229001",
            //"229002",
            //"229003",
            //"229004",
            //"229005",
            //"229006",
            //"229007",
            //"228981",
            //"228982",
            //"228983",
            //"228984",
            //"228985",
            //"228986",
            //"228987",
            //"228988",
            //"228989",
            //"228990"
        };

        public LanCacheLogReaderHostedService(IServiceProvider services,
            DeveLanCacheConfiguration deveLanCacheConfiguration,
            ILogger<LanCacheLogReaderHostedService> logger)
        {
            _services = services;
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
            var oldestLog = DateTime.MinValue;
            await using (var scope = _services.CreateAsyncScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
                var lastUpdatedItem = await dbContext.DownloadEvents.OrderByDescending(t => t.LastUpdatedAt).FirstOrDefaultAsync();
                if (lastUpdatedItem != null)
                {
                    oldestLog = lastUpdatedItem.LastUpdatedAt;
                }
                var totalByteReadSetting = await dbContext.Settings.FirstOrDefaultAsync(t => t.Key == DbSetting.SettingKey_TotalBytesRead);

                if (_deveLanCacheConfiguration.Feature_SkipLinesBasedOnBytesRead && long.TryParse(totalByteReadSetting?.Value, out var result))
                {
                    TotalBytesRead = result;
                }
            }

            var logFilePath = _deveLanCacheConfiguration.LanCacheLogsDirectory;
            if (logFilePath == null)
            {
                throw new NullReferenceException("LanCacheLogsDirectory == null, please ensure the LanCacheLogsDirectory ENVIRONMENT_VARIABLE is filled in");
            }
            var accessLogFilePath = Path.Combine(logFilePath, "access.log");

            using (var fileStream = new FileStream(accessLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var allLogLines = TailFrom2(fileStream, stoppingToken);
                var parsedLogLines = allLogLines.Select(t => t == null ? null : LanCacheLogLineParser.ParseLogEntry(t));
                var batches = Batch2(parsedLogLines, 5000, oldestLog);

                int totalLinesProcessed = 0;

                foreach (var currentSet in batches)
                {
                    _logger.LogInformation("Processing {Count} lines... First DateTime: {FirstDate} (Total processed: {TotalLinesProcessed})",
                        currentSet.Count, currentSet.FirstOrDefault()?.DateTime, totalLinesProcessed);
                    totalLinesProcessed += currentSet.Count;

                    await using (var scope = _services.CreateAsyncScope())
                    {
                        var retryPolicy = Policy
                            .Handle<DbUpdateException>()
                            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                            (exception, timeSpan, context) =>
                            {
                                _logger.LogError("An error occurred while trying to save changes: {Message}", exception.Message);
                            });

                        await retryPolicy.ExecuteAsync(async () =>
                        {
                            using var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

                            //var filteredLogLines = currentSet.Where(t => t.CacheIdentifier == "steam");
                            IEnumerable<LanCacheLogEntryRaw> filteredLogLines = currentSet;
                            filteredLogLines = filteredLogLines.Where(t => t.CacheIdentifier != "127.0.0.1");
                            filteredLogLines = filteredLogLines.Where(t => t.Referer != SkipLogLineReferrerString);

                            Dictionary<string, DbDownloadEvent> steamAppDownloadEventsCache = new Dictionary<string, DbDownloadEvent>();

                            foreach (var lanCacheLogLine in filteredLogLines)
                            {
                                if (lanCacheLogLine.CacheIdentifier == "steam" && ExcludedAppIds.Contains(lanCacheLogLine.DownloadIdentifier))
                                {
                                    continue;
                                }

                                if (
                                    (lanCacheLogLine.CacheIdentifier == "steam" && lanCacheLogLine.Request.Contains("/manifest/")) ||
                                    (lanCacheLogLine.CacheIdentifier == "epicgames" && lanCacheLogLine.Request.Contains(".manifest?"))
                                    )
                                {
                                    if (DateTime.Now < lanCacheLogLine.DateTime.AddDays(14))
                                    {
                                        // We found a manifest. This contains more information about the download. We'll process these asynchronously
                                        // since it requires downloading and parsing the manifest file.
                                        _logger.LogInformation("Found manifest for download: {DownloadIdentifier}", lanCacheLogLine.DownloadIdentifier);
                                        var itemToProcessAsynchronously = new DbAsyncLogEntryProcessingQueueItem()
                                        {
                                            LanCacheLogEntryRaw = lanCacheLogLine
                                        };
                                        dbContext.AsyncLogEntryProcessingQueueItems.Add(itemToProcessAsynchronously);
                                    }
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
                                    _logger.LogInformation("Adding new event because more than 5 minutes no update: {CacheKey} ({DateTime})", cacheKey, lanCacheLogLine.DateTime);

                                    uint.TryParse(lanCacheLogLine.DownloadIdentifier, out var downloadIdentifierInt);
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

                            //Save total bytes read
                            var totalByteReadSetting = await dbContext.Settings.FirstOrDefaultAsync(t => t.Key == DbSetting.SettingKey_TotalBytesRead);
                            if (totalByteReadSetting == null)
                            {
                                totalByteReadSetting = new DbSetting()
                                {
                                    Key = DbSetting.SettingKey_TotalBytesRead
                                };
                                await dbContext.Settings.AddAsync(totalByteReadSetting);
                            }
                            totalByteReadSetting.Value = TotalBytesRead.ToString();

                            await dbContext.SaveChangesAsync();
                            FrontendRefresherService.RequireFrontendRefresh();
                        });
                    }
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
                            _logger.LogInformation("{Now} No new log lines, waiting...", DateTime.Now);
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
                        _logger.LogInformation("Skipped total of {SkipCounter} lines (already processed)", skipCounter);
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

        public long TotalBytesRead { get; set; }

        public IEnumerable<string> TailFrom2(Stream inputStream, CancellationToken stoppingToken)
        {
            if (inputStream.Length >= TotalBytesRead)
            {
                inputStream.Position = TotalBytesRead;
            }
            else
            {
                TotalBytesRead = 0;
            }

            const int BufferSize = 1024;
            var buffer = new byte[BufferSize];
            var leftoverBuffer = new List<byte>();
            int bytesRead;

            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();

                bytesRead = inputStream.Read(buffer, 0, BufferSize);

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
                    var hasRAtTheEnd = newlineIndex > 0 ? buffer[newlineIndex - 1] == '\r' : (leftoverBuffer.Count > 0 ? leftoverBuffer[^1] == '\r' : false);
                    var lineEndIndex = hasRAtTheEnd ? newlineIndex - 1 : newlineIndex;

                    var lineBuffer = new byte[leftoverBuffer.Count + lineEndIndex - searchStartIndex];
                    leftoverBuffer.CopyTo(0, lineBuffer, 0, Math.Min(lineBuffer.Length, leftoverBuffer.Count));
                    if (lineEndIndex - searchStartIndex > 0)
                    {
                        Array.Copy(buffer, searchStartIndex, lineBuffer, leftoverBuffer.Count, lineEndIndex - searchStartIndex);
                    }

                    TotalBytesRead += lineBuffer.Length + (hasRAtTheEnd ? 1 : 0) + 1;
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
