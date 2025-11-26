using System.Net;

namespace DeveLanCacheUI_Backend.EpicManifestParser;
// ReSharper disable UseSymbolAlias

/// <summary>
/// Options/Configuration for parsing manifests
/// </summary>
public class ManifestParseOptions
{
    /// <summary>
    /// Zlib decompress delegate.
    /// </summary>
    public delegate bool DecompressDelegate(object? state, byte[] source, int sourceOffset, int sourceLength, byte[] destination, int destinationOffset, int destinationLength);

    /// <summary>
    /// Zlib decompress delegate, defaults to <see cref="ManifestZlibStreamDecompressor.Decompress"/>.
    /// </summary>
    public DecompressDelegate? Decompressor { get; set; } = ManifestZlibStreamDecompressor.Decompress;

    /// <summary>
    /// Optional state that gets passed to the <see cref="Decompressor"/> delegate.
    /// </summary>
    public object? DecompressorState { get; set; }

    /// <summary>
    /// Required for downloading, must have a leading slash!
    /// </summary>
    /// <remarks>
    /// Example: <code>http://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/</code><br/>
    /// Distributionpoints can be found here: <see href="https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/distributionpoints">here.</see>
    /// </remarks>
    public string? ChunkBaseUrl { get; set; }

    /// <summary>
    /// Your own (optional) <see cref="HttpClient"/> used for downloading, must not have a <see cref="HttpClient.BaseAddress"/> !
    /// </summary>
    public HttpClient? Client { get; set; }

    /// <summary>
    /// Buffer size for downloading chunks, defaults to <value>2097152</value> bytes (2 MiB).
    /// </summary>
    public int ChunkDownloadBufferSize { get; set; } = 2097152;

    /// <summary>
    /// Optional for caching chunks, very recommended.
    /// </summary>
    public string? ChunkCacheDirectory { get; set; }

    /// <summary>
    /// Whether or not to cache the chunks 1:1 as they were downloaded, defaults to <see langword="false"/>.
    /// </summary>
    public bool CacheChunksAsIs { get; set; }

    /// <summary>
    /// Optional for caching manifests when using <see cref="ManifestInfo.DownloadAndParseAsync(ManifestParseOptions, Predicate&lt;ManifestInfoElement&gt;?, Predicate&lt;ManifestInfoElementManifest&gt;?, CancellationToken)"/>.
    /// </summary>
    public string? ManifestCacheDirectory { get; set; }

    /// <summary>
    /// Creates a default <see cref="HttpClient"/> and also sets <see cref="Client"/> to its instance.
    /// </summary>
    /// <returns>The created <see cref="HttpClient"/>.</returns>
    public HttpClient CreateDefaultClient()
    {
        if (Client is not null)
            return Client;

        var handler = new SocketsHttpHandler
        {
            UseCookies = false,
            UseProxy = false,
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            MaxConnectionsPerServer = 256
        };
        Client = new HttpClient(handler)
        {
            DefaultRequestVersion = new Version(1, 1),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Timeout = TimeSpan.FromSeconds(30)
        };
        Client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("EpicGamesLauncher/16.13.0-36938137+++Portal+Release-Live Windows/10.0.26100.1.256.64bit");
        Client.DefaultRequestHeaders.ConnectionClose = false;
        return Client;
    }
}
