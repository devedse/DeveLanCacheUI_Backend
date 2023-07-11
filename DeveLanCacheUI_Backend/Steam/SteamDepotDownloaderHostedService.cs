using DeveLanCacheUI_Backend.Db;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;

namespace DeveLanCacheUI_Backend.Steam
{
    public class SteamDepotDownloaderHostedService : BackgroundService
    {
        public IServiceProvider Services { get; }

        private readonly IConfiguration _configuration;
        private readonly ILogger<SteamDepotDownloaderHostedService> _logger;
        private readonly HttpClient _httpClient;

        private const string DeveLanCacheUISteamDepotFinderLatestUrl = "https://api.github.com/repos/devedse/DeveLanCacheUI_SteamDepotFinder_Runner/releases/latest";

        public SteamDepotDownloaderHostedService(IServiceProvider services,
            IConfiguration configuration,
            ILogger<SteamDepotDownloaderHostedService> logger)
        {
            Services = services;
            _configuration = configuration;
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
            var depotFileDirectory = _configuration.GetValue<string>("DepotFileDirectory")!;

            if (string.IsNullOrWhiteSpace(depotFileDirectory))
            {
                depotFileDirectory = Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(depotFileDirectory))
            {
                Directory.CreateDirectory(depotFileDirectory);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var shouldDownload = await NewDepotFileAvailable();

                if (shouldDownload.NewVersionAvailable)
                {
                    await GoDownload(depotFileDirectory, shouldDownload);
                }

                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        private async Task GoDownload(string depotFileDirectory, (bool NewVersionAvailable, Version? LatestVersion, string? DownloadUrl) shouldDownload)
        {
            _logger.LogInformation($"Detected that new version '{shouldDownload.NewVersionAvailable}' is available, so downloading: {shouldDownload.DownloadUrl}...");

            var downloadedCsv = await _httpClient.GetAsync(shouldDownload.DownloadUrl);
            if (!downloadedCsv.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Could not obtain {DeveLanCacheUISteamDepotFinderLatestUrl}: {downloadedCsv.StatusCode}, {downloadedCsv.ReasonPhrase}");
                return;
            }

            var bytes = await downloadedCsv.Content.ReadAsByteArrayAsync();

            var fileName = $"depot_{shouldDownload.LatestVersion}.csv";
            var filePath = Path.Combine(depotFileDirectory, fileName);
            File.WriteAllBytes(filePath, bytes);
        }

        private async Task<(bool NewVersionAvailable, Version? LatestVersion, string? DownloadUrl)> NewDepotFileAvailable()
        {
            Version? currentVersion = null;

            //User-Agent: request
            var latestStatus = await _httpClient.GetAsync(DeveLanCacheUISteamDepotFinderLatestUrl);
            if (!latestStatus.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Could not obtain {DeveLanCacheUISteamDepotFinderLatestUrl}: {latestStatus.StatusCode}, {latestStatus.ReasonPhrase}");
                return (false, null, null);
            }

            var data = await latestStatus.Content.ReadAsStringAsync();
            var dataParsed = JsonSerializer.Deserialize<GithubLatestApiPoco>(data);

            if (dataParsed == null)
            {
                _logger.LogWarning($"Could not parse data for depots: {data}");
                return (false, null, null);
            }

            if (!Version.TryParse(dataParsed.name, out var latestVersion))
            {
                _logger.LogWarning($"Could not parse version for depots: {dataParsed.name}");
                return (false, null, null);
            }

            var downloadUrl = dataParsed.assets.FirstOrDefault(t => t.browser_download_url.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))?.browser_download_url;
            if (downloadUrl == null)
            {
                _logger.LogWarning($"Could not find download url in: {data}");
                return (false, null, null);
            }

            await using (var scope = Services.CreateAsyncScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                var foundSetting = await dbContext.Settings.FirstOrDefaultAsync();
                if (foundSetting == null || foundSetting.Value == null)
                {
                    return (true, latestVersion, downloadUrl);
                }

                if (!Version.TryParse(foundSetting.Value, out currentVersion))
                {
                    return (true, latestVersion, downloadUrl);
                }
            }

            if (latestVersion > currentVersion)
            {
                return (true, latestVersion, downloadUrl);
            }
            return (false, null, null);
        }
    }
}
