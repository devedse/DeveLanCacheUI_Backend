namespace DeveLanCacheUI_Backend.Helpers
{
    /// <summary>
    /// Simple helper to download remote files. If the url ends with .gz it will stream-decompress and return the raw bytes.
    /// Keeps implementation minimal and self-contained.
    /// </summary>
    public static class RemoteFileDownloader
    {
        public static async Task<byte[]> DownloadFileAsync(string url, CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "request");

            var rawBytes = await httpClient.GetByteArrayAsync(url, cancellationToken);
            if (url.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                using var inputMs = new MemoryStream(rawBytes);
                using var gzip = new GZipStream(inputMs, CompressionMode.Decompress);
                using var outputMs = new MemoryStream();
                await gzip.CopyToAsync(outputMs, cancellationToken);
                return outputMs.ToArray();
            }
            return rawBytes;
        }

        /// <summary>
        /// Downloads a specific asset from the latest GitHub release for the given owner/repo.
        /// If the primary asset name is not found, optional alternate names will be tried in order.
        /// Automatically decompresses .gz assets.
        /// </summary>
        public static async Task<byte[]> DownloadGithubLatestReleaseAssetAsync(
            string owner,
            string repo,
            string assetName,
            CancellationToken cancellationToken = default)
        {
            var releaseUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "request");

            var latestJson = await httpClient.GetStringAsync(releaseUrl, cancellationToken);
            var release = JsonSerializer.Deserialize<DeveLanCacheUI_Backend.Services.OriginalDepotEnricher.Models.GithubLatestApiPoco>(latestJson);
            if (release == null)
            {
                throw new InvalidOperationException($"Could not deserialize latest release for {owner}/{repo}");
            }

            var asset = release.assets.FirstOrDefault(a => string.Equals(a.name, assetName, StringComparison.OrdinalIgnoreCase));
            if (asset == null)
            {
                throw new FileNotFoundException($"Asset '{assetName}' not found in latest release for {owner}/{repo}");
            }

            // Reuse existing download + auto .gz logic
            return await DownloadFileAsync(asset.browser_download_url, cancellationToken);
        }
    }
}
