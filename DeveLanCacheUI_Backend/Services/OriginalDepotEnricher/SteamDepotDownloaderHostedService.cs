﻿using DeveLanCacheUI_Backend.Services.OriginalDepotEnricher.Models;

namespace DeveLanCacheUI_Backend.Services.OriginalDepotEnricher
{
    public class SteamDepotDownloaderHostedService : BackgroundService
    {
        public IServiceProvider Services { get; }

        private readonly DeveLanCacheConfiguration _deveLanCacheConfiguration;
        private readonly SteamDepotEnricherHostedService _steamDepotEnricherHostedService;
        private readonly ILogger<SteamDepotDownloaderHostedService> _logger;
        private readonly HttpClient _httpClient;

        private const string DeveLanCacheUISteamDepotFinderLatestUrl = "https://api.github.com/repos/devedse/DeveLanCacheUI_SteamDepotFinder_Runner/releases/latest";

        public SteamDepotDownloaderHostedService(IServiceProvider services,
            DeveLanCacheConfiguration deveLanCacheConfiguration,
            SteamDepotEnricherHostedService steamDepotEnricherHostedService,
            ILogger<SteamDepotDownloaderHostedService> logger)
        {
            Services = services;
            _deveLanCacheConfiguration = deveLanCacheConfiguration;
            _steamDepotEnricherHostedService = steamDepotEnricherHostedService;
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "request");
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

            if (Directory.Exists(depotFileDirectory))
            {
                // This is purely cleanup since we don't use the depot directory anymore. We just directly download and process.
                Directory.Delete(depotFileDirectory, true);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var shouldDownload = await NewDepotFileAvailable();

                _logger.LogInformation("New Depot File Available: {NewVersionAvailable}, LatestVersion: {LatestVersion}, DownloadUrl: {DownloadUrl}",
                    shouldDownload.NewVersionAvailable, shouldDownload.LatestVersion, shouldDownload.DownloadUrl);

                if (shouldDownload.NewVersionAvailable)
                {
                    if (shouldDownload.LatestVersion == null || string.IsNullOrWhiteSpace(shouldDownload.DownloadUrl))
                    {
                        throw new UnreachableException($"New version available, but LatestVersion or DownloadUrl is null. This should not happen. LatestVersion: {shouldDownload.LatestVersion}, DownloadUrl: {shouldDownload.DownloadUrl}");
                    }
                    var downloadedBytes = await GoDownload(depotFileDirectory, shouldDownload);

                    if (downloadedBytes != null)
                    {
                        await _steamDepotEnricherHostedService.GoProcess(shouldDownload.LatestVersion, downloadedBytes, stoppingToken);
                    }
                    else
                    {
                        _logger.LogWarning("Downloaded depot file was null, not processing further.");
                    }
                }

                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        private async Task<byte[]?> GoDownload(string depotFileDirectory, (bool NewVersionAvailable, Version? LatestVersion, string? DownloadUrl) shouldDownload)
        {
            _logger.LogInformation("Detected that new version '{NewVersionAvailable}' of Depot File is available, so downloading: {DownloadUrl}...", shouldDownload.NewVersionAvailable, shouldDownload.DownloadUrl);

            var downloadedCsv = await _httpClient.GetAsync(shouldDownload.DownloadUrl);
            if (!downloadedCsv.IsSuccessStatusCode)
            {
                _logger.LogWarning("Could not obtain {Url}: {StatusCode}, {ReasonPhrase}", DeveLanCacheUISteamDepotFinderLatestUrl, downloadedCsv.StatusCode, downloadedCsv.ReasonPhrase);
                return null;
            }

            var bytes = await downloadedCsv.Content.ReadAsByteArrayAsync();
            return bytes;
        }

        private async Task<(bool NewVersionAvailable, Version? LatestVersion, string? DownloadUrl)> NewDepotFileAvailable()
        {
            Version? currentVersion = null;

            //User-Agent: request
            var latestStatus = await _httpClient.GetAsync(DeveLanCacheUISteamDepotFinderLatestUrl);
            if (!latestStatus.IsSuccessStatusCode)
            {
                _logger.LogWarning("Could not obtain {Url}: {StatusCode}, {ReasonPhrase}", DeveLanCacheUISteamDepotFinderLatestUrl, latestStatus.StatusCode, latestStatus.ReasonPhrase);
                return (false, null, null);
            }

            var data = await latestStatus.Content.ReadAsStringAsync();
            var dataParsed = JsonSerializer.Deserialize<GithubLatestApiPoco>(data);

            if (dataParsed == null)
            {
                _logger.LogWarning("Could not parse data for depots: {Data}", data);
                return (false, null, null);
            }

            if (!Version.TryParse(dataParsed.name, out var latestVersion))
            {
                _logger.LogWarning("Could not parse version for depots: {VersionName}", dataParsed.name);
                return (false, null, null);
            }

            var downloadUrl = dataParsed.assets.FirstOrDefault(t => t.browser_download_url.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))?.browser_download_url;
            if (downloadUrl == null)
            {
                _logger.LogWarning("Could not find download url in: {Data}", data);
                return (false, null, null);
            }

            await using (var scope = Services.CreateAsyncScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                var foundSetting = await dbContext.Settings.FirstOrDefaultAsync(t => t.Key == DbSetting.SettingKey_DepotVersion);
                if (foundSetting == null || foundSetting.Value == null)
                {
                    _logger.LogInformation("Update of Depot File required because CurrentVersion could not be found");
                    return (true, latestVersion, downloadUrl);
                }

                if (!Version.TryParse(foundSetting.Value, out currentVersion))
                {
                    _logger.LogInformation("Update of Depot File required because CurrentVersion could not be parsed: {CurrentVersion}", foundSetting.Value);
                    return (true, latestVersion, downloadUrl);
                }
            }

            if (latestVersion > currentVersion)
            {
                _logger.LogInformation("Update of Depot File required because LatestVersion ({LatestVersion}) > CurrentVersion ({CurrentVersion})", latestVersion, currentVersion);
                return (true, latestVersion, downloadUrl);
            }
            return (false, null, null);
        }
    }
}
