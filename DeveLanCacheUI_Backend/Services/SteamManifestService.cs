namespace DeveLanCacheUI_Backend.Services
{
    public class SteamManifestService
    {
        private readonly IServiceProvider _services;
        private readonly IHttpClientFactory _httpClientFactoryForManifestDownloads;
        private readonly ILogger<SteamManifestService> _logger;
        private readonly string _manifestDirectory;

        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public SteamManifestService(DeveLanCacheConfiguration deveLanCacheConfiguration, IServiceProvider services, IHttpClientFactory httpClientFactory, ILogger<SteamManifestService> logger)
        {
            _services = services;
            _httpClientFactoryForManifestDownloads = httpClientFactory;
            _logger = logger;

            var deveLanCacheUIDataDirectory = deveLanCacheConfiguration.DeveLanCacheUIDataDirectory ?? string.Empty;
            _manifestDirectory = Path.Combine(deveLanCacheUIDataDirectory, "manifests");
        }

        public void TryToDownloadManifest(LanCacheLogEntryRaw lanCacheLogEntryRaw)
        {
            //This method could use some TPL Dataflow, I now use locking which should be okayish

            if (!lanCacheLogEntryRaw.Request.Contains("/manifest/") || lanCacheLogEntryRaw.DownloadIdentifier == null)
            {
                _logger.LogError("Code bug: Trying to download manifest that isn't actually a manifest: {OriginalLogLine}", lanCacheLogEntryRaw.OriginalLogLine);
                return;
            }

            _ = Task.Run(async () =>
            {
                var fallbackPolicy = Policy
                    .Handle<Exception>()
                    .FallbackAsync(async (ct) =>
                    {
                        await Task.CompletedTask;
                        _logger.LogInformation("Manifest saving: All retries failed, skipping...");
                    });

                var retryPolicy = Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                   (exception, timeSpan, context) =>
                   {
                       _logger.LogInformation("Manifest saving: An error occurred while trying to save changes: {Message}", exception.Message);
                   });

                await fallbackPolicy.WrapAsync(retryPolicy).ExecuteAsync(async () =>
                {
                    try
                    {
                        _semaphoreSlim.Wait();
                        await using (var scope = _services.CreateAsyncScope())
                        {
                            var theManifestUrlPart = lanCacheLogEntryRaw.Request.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];

                            var everythingAfterManifest = theManifestUrlPart.Split("/manifest/", StringSplitOptions.RemoveEmptyEntries).Last();
                            var manifestId = everythingAfterManifest.Split("/", StringSplitOptions.RemoveEmptyEntries).First();

                            //Replace invalid chars should dissalow reading any file you want :)
                            var manifestIdFileName = RemoveNonNumericCharacters(manifestId) + ".bin";
                            var depotId = RemoveNonNumericCharacters(lanCacheLogEntryRaw.DownloadIdentifier!);
                            var depotIdAndManifestIdentifier = Path.Combine(depotId, manifestIdFileName);


                            using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                            var dbManifestFound = await dbContext.SteamManifests.FirstOrDefaultAsync(t => t.UniqueManifestIdentifier == depotIdAndManifestIdentifier);

                            if (dbManifestFound != null)
                            {
                                return;
                            }

                            var fullPath = Path.Combine(_manifestDirectory, depotIdAndManifestIdentifier);
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);


                            var cachedUrl = $"http://lancache.steamcontent.com{theManifestUrlPart}";
                            using var httpClient = _httpClientFactoryForManifestDownloads.CreateClient();
                            httpClient.DefaultRequestHeaders.Add("Host", lanCacheLogEntryRaw.Host);
                            httpClient.DefaultRequestHeaders.Add("User-Agent", "Valve/Steam HTTP Client 1.0");
                            httpClient.DefaultRequestHeaders.Referrer = LanCacheLogReaderHostedService.SkipLogLineReferrer; //Add this to ensure we don't process this line again
                            var manifestResponse = await httpClient.GetAsync(cachedUrl);





                            if (!manifestResponse.IsSuccessStatusCode)
                            {
                                _logger.LogWarning("Warning: Tried to obtain manifest for: {DownloadIdentifier} but status code was: {StatusCode}", lanCacheLogEntryRaw.DownloadIdentifier, manifestResponse.StatusCode);
                                return;
                            }
                            var manifestBytes = await manifestResponse.Content.ReadAsByteArrayAsync();


                            var dbManifest = ManifestBytesToDbSteamManifest(manifestBytes, depotIdAndManifestIdentifier);

                            if (dbManifest == null)
                            {
                                _logger.LogWarning("Could not get manifest for depot: {DownloadIdentifier}", lanCacheLogEntryRaw.DownloadIdentifier);
                                return;
                            }

                            var dbValue = dbContext.SteamManifests.FirstOrDefault(t => t.DepotId == dbManifest.DepotId && t.CreationTime == dbManifest.CreationTime);
                            if (dbValue != null)
                            {
                                dbContext.Entry(dbValue).CurrentValues.SetValues(dbManifest);
                                _logger.LogInformation("Updated manifest for {DownloadIdentifier}", lanCacheLogEntryRaw.DownloadIdentifier);
                            }
                            else
                            {
                                await dbContext.SteamManifests.AddAsync(dbManifest);
                                _logger.LogInformation("Added manifest for {DownloadIdentifier}", lanCacheLogEntryRaw.DownloadIdentifier);
                            }

                            await File.WriteAllBytesAsync(fullPath, manifestBytes);
                            await dbContext.SaveChangesAsync();
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }
                });
            });
        }

        private string RemoveNonNumericCharacters(string input)
        {
            return new string(input.Where(c => char.IsDigit(c)).ToArray());
        }

        private DbSteamManifest? ManifestBytesToDbSteamManifest(byte[] manifestBytes, string uniqueManifestIdentifier)
        {
            var bytesDecompressed = ZipUtil.Decompress(manifestBytes);
            var depotManifest = DepotManifest.Deserialize(bytesDecompressed);

            if (depotManifest == null)
            {
                return null;
            }

            var dbSteamManifest = new DbSteamManifest()
            {
                DepotId = depotManifest.DepotID,
                CreationTime = depotManifest.CreationTime,
                TotalCompressedSize = depotManifest.TotalCompressedSize,
                TotalUncompressedSize = depotManifest.TotalUncompressedSize,
                ManifestBytesSize = (ulong)manifestBytes.LongLength,
                UniqueManifestIdentifier = uniqueManifestIdentifier
            };

            return dbSteamManifest;
        }

        public static string ManifestBytesToJson(byte[] manifestBytes, bool indented)
        {
            var bytesDecompressed = ZipUtil.Decompress(manifestBytes);
            var depotManifest = DepotManifest.Deserialize(bytesDecompressed);
            var json = JsonSerializer.Serialize(depotManifest, new JsonSerializerOptions() { WriteIndented = indented });
            return json;
        }

        public static JsonDocument ManifestBytesToJsonValue(byte[] manifestBytes)
        {
            var json = ManifestBytesToJson(manifestBytes, false);
            var document = JsonDocument.Parse(json);
            return document;
        }

        //public string DecodeBase64(byte[] data)
        //{
        //    string chunkIDHex = BitConverter.ToString(data).Replace("-", "").ToLower();
        //    return chunkIDHex;
        //}

        public byte[]? GetBytesForUniqueManifestIdentifier(string uniqueManifestIdentifier)
        {
            var fullPath = Path.Combine(_manifestDirectory, uniqueManifestIdentifier);
            if (File.Exists(fullPath))
            {
                return File.ReadAllBytes(fullPath);
            }
            else
            {
                return null;
            }
        }
    }
}
