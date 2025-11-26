
namespace DeveLanCacheUI_Backend.Services
{
    public class EpicManifestService
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactoryForManifestDownloads;
        private readonly ILogger<EpicManifestService> _logger;
        private readonly string _manifestDirectory;

        public EpicManifestService(
            DeveLanCacheConfiguration deveLanCacheConfiguration,
            DeveLanCacheUIDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            ILogger<EpicManifestService> logger)
        {
            _dbContext = dbContext;
            _httpClientFactoryForManifestDownloads = httpClientFactory;
            _logger = logger;

            var deveLanCacheUIDataDirectory = deveLanCacheConfiguration.DeveLanCacheUIDataDirectory ?? string.Empty;
            _manifestDirectory = Path.Combine(deveLanCacheUIDataDirectory, "manifests");
        }

        public async Task TryToDownloadManifest(LanCacheLogEntryRaw lanCacheLogEntryRaw)
        {
            if (lanCacheLogEntryRaw.CacheIdentifier != "epicgames" || !lanCacheLogEntryRaw.Request.Contains(".manifest?"))
            {
                _logger.LogError("Code bug: Trying to download manifest that isn't actually a manifest: {OriginalLogLine}", lanCacheLogEntryRaw.OriginalLogLine);
                return;
            }

            var firstItem = await _dbContext.EpicManifests.FirstOrDefaultAsync(t => t.DownloadIdentifier == lanCacheLogEntryRaw.DownloadIdentifier);
            if (firstItem != null)
            {
                return;
            }


            var theManifestUrlPart = lanCacheLogEntryRaw.Request.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
            var fullManifestUrl = $"http://{lanCacheLogEntryRaw.Host}{theManifestUrlPart}";

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

                using var httpClient = _httpClientFactoryForManifestDownloads.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Host", lanCacheLogEntryRaw.Host);
                httpClient.DefaultRequestHeaders.Add("User-Agent", lanCacheLogEntryRaw.UserAgent);
                httpClient.DefaultRequestHeaders.Referrer = LanCacheLogReaderHostedService.SkipLogLineReferrer; //Add this to ensure we don't process this line again
                var manifestResponse = await httpClient.GetAsync(fullManifestUrl);

                if (!manifestResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Warning: Tried to obtain manifest for: {DownloadIdentifier} but status code was: {StatusCode}", lanCacheLogEntryRaw.DownloadIdentifier, manifestResponse.StatusCode);
                    return;
                }
                var manifestBytes = await manifestResponse.Content.ReadAsByteArrayAsync();

                var parsedManifest = EpicManifestParser.EpicManifestParser.Deserialize(manifestBytes);

                _logger.LogInformation("Parsed LogLine from: {LogLineDateTime}, Epic Manifest for AppId: '{AppId}' AppName: '{AppName}' Launch Exe: '{Manifest}'", lanCacheLogEntryRaw.DateTime, parsedManifest.Meta.AppID, parsedManifest.Meta.AppName, parsedManifest.Meta.LaunchExe);

                var epicManifest = new DbEpicManifest()
                {
                    Name = Path.GetFileNameWithoutExtension(parsedManifest.Meta.LaunchExe),
                    DownloadIdentifier = lanCacheLogEntryRaw.DownloadIdentifier,
                    CreationTime = DateTime.UtcNow,
                    ManifestBytesSize = (ulong)manifestBytes.Length,
                    TotalCompressedSize = (ulong)parsedManifest.TotalDownloadSize,
                    TotalUncompressedSize = (ulong)parsedManifest.TotalBuildSize
                };

                await _dbContext.EpicManifests.AddAsync(epicManifest);
                await _dbContext.SaveChangesAsync();
            });
        }
    }
}
