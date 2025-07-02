using DeveLanCacheUI_Backend.EpicManifestParser.UE;
using Flurl;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DeveLanCacheUI_Backend.EpicManifestParser.Api;
// ReSharper disable UseSymbolAlias

/// <summary/>
public class ManifestInfo
{
    /// <summary/>
    public required List<ManifestInfoElement> Elements { get; set; }

    /// <summary>
    /// Parses the UTF-8 encoded text representing a single JSON value into a <see cref="ManifestInfo"/>.
    /// </summary>
    /// <returns>A <see cref="ManifestInfo"/> representation of the JSON value.</returns>
    /// <param name="utf8Json">JSON text to parse.</param>
    /// <exception cref="JsonException">
    /// The JSON is invalid,
    /// <see cref="ManifestInfo"/> is not compatible with the JSON,
    /// or when there is remaining data in the buffer.
    /// </exception>
    public static ManifestInfo? Deserialize(ReadOnlySpan<byte> utf8Json)
        => JsonSerializer.Deserialize(utf8Json, EpicManifestParserJsonContext.Default.ManifestInfo);

    /// <summary>
    /// Reads the UTF-8 encoded text representing a single JSON value into a <see cref="ManifestInfo"/>.
    /// The Stream will be read to completion.
    /// </summary>
    /// <returns>A <see cref="ManifestInfo"/> representation of the JSON value.</returns>
    /// <param name="utf8Json">JSON data to parse.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="utf8Json"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    /// The JSON is invalid,
    /// <see cref="ManifestInfo"/> is not compatible with the JSON,
    /// or when there is remaining data in the Stream.
    /// </exception>
    public static ManifestInfo? Deserialize(Stream utf8Json)
        => JsonSerializer.Deserialize(utf8Json, EpicManifestParserJsonContext.Default.ManifestInfo);

    /// <summary>
    /// Reads the UTF-8 encoded text representing a single JSON value into a <see cref="ManifestInfo"/>.
    /// The Stream will be read to completion.
    /// </summary>
    /// <returns>A <see cref="ManifestInfo"/> representation of the JSON value.</returns>
    /// <param name="utf8Json">JSON data to parse.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> that can be used to cancel the read operation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="utf8Json"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    /// The JSON is invalid,
    /// <see cref="ManifestInfo"/> is not compatible with the JSON,
    /// or when there is remaining data in the Stream.
    /// </exception>
    public static ValueTask<ManifestInfo?> DeserializeAsync(Stream utf8Json, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsync(utf8Json, EpicManifestParserJsonContext.Default.ManifestInfo, cancellationToken);

    /// <summary>
    /// Reads the UTF-8 encoded text representing a single JSON value into a <see cref="ManifestInfo"/>.
    /// The Stream will be read to completion.
    /// </summary>
    /// <returns>A <see cref="ManifestInfo"/> representation of the JSON value.</returns>
    /// <param name="path">JSON file to parse.</param>
    /// <exception cref="JsonException">
    /// The JSON is invalid,
    /// <see cref="ManifestInfo"/> is not compatible with the JSON,
    /// or when there is remaining data in the Stream.
    /// </exception>
    /// <inheritdoc cref="File.OpenRead(string)"/>
    public static ManifestInfo? DeserializeFile(string path)
    {
        using var fs = File.OpenRead(path);
        return JsonSerializer.Deserialize(fs, EpicManifestParserJsonContext.Default.ManifestInfo);
    }

    /// <summary>
    /// Reads the UTF-8 encoded text representing a single JSON value into a <see cref="ManifestInfo"/>.
    /// The Stream will be read to completion.
    /// </summary>
    /// <returns>A <see cref="ManifestInfo"/> representation of the JSON value.</returns>
    /// <param name="path">JSON file to parse.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> that can be used to cancel the read operation.
    /// </param>
    /// <exception cref="JsonException">
    /// The JSON is invalid,
    /// <see cref="ManifestInfo"/> is not compatible with the JSON,
    /// or when there is remaining data in the Stream.
    /// </exception>
    /// <inheritdoc cref="File.OpenRead(string)"/>
    public static ValueTask<ManifestInfo?> DeserializeFileAsync(string path, CancellationToken cancellationToken = default)
    {
        using var fs = File.OpenRead(path);
        return JsonSerializer.DeserializeAsync(fs, EpicManifestParserJsonContext.Default.ManifestInfo, cancellationToken);
    }

    /// <param name="elementPredicate">Predicate to select the a single element in <see cref="Elements"/></param>
    /// <param name="elementManifestPredicate">Predicate to select the a single manifest in <see cref="ManifestInfoElement.Manifests"/></param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> that can be used to cancel the read operation.
    /// </param>
    /// <param name="optionsBuilder">Builder for options for parsing and/or caching the manifest</param>
    /// <inheritdoc cref="DownloadAndParseAsync(ManifestParseOptions, Predicate&lt;ManifestInfoElement&gt;?, Predicate&lt;ManifestInfoElementManifest&gt;?, CancellationToken)"/>
    public Task<(FBuildPatchAppManifest Manifest, ManifestInfoElement InfoElement)> DownloadAndParseAsync(
        Predicate<ManifestInfoElement>? elementPredicate = null, Predicate<ManifestInfoElementManifest>? elementManifestPredicate = null,
        CancellationToken cancellationToken = default, Action<ManifestParseOptions>? optionsBuilder = null)
    {
        var options = new ManifestParseOptions();
        optionsBuilder?.Invoke(options);
        return DownloadAndParseAsync(options, elementPredicate, elementManifestPredicate, cancellationToken);
    }

    /// <summary>
    /// Downloads and parses the manifest.
    /// </summary>
    /// <param name="options">Options for parsing and/or caching the manifest</param>
    /// <param name="elementPredicate">Predicate to select the a single element in <see cref="Elements"/></param>
    /// <param name="elementManifestPredicate">Predicate to select the a single manifest in <see cref="ManifestInfoElement.Manifests"/></param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> that can be used to cancel the read operation.
    /// </param>
    /// <returns>
    /// The parsed <see cref="FBuildPatchAppManifest"/> manifest and the selected <see cref="ManifestInfoElement"/> info element in a <see cref="ValueTuple"/>
    /// </returns>
    /// <exception cref="InvalidOperationException">When a predicate fails.</exception>
    /// <exception cref="HttpRequestException">When the manifest data fails to download.</exception>
    public async Task<(FBuildPatchAppManifest Manifest, ManifestInfoElement InfoElement)> DownloadAndParseAsync(
        ManifestParseOptions options, Predicate<ManifestInfoElement>? elementPredicate = null,
        Predicate<ManifestInfoElementManifest>? elementManifestPredicate = null, CancellationToken cancellationToken = default)
    {
        ManifestInfoElement element;
        if (elementPredicate is null)
            element = Elements[0];
        else
            element = Elements.Find(elementPredicate) ?? throw new InvalidOperationException("Could not find ManifestInfoElement based on predicate");

        ManifestInfoElementManifest elementManifest;
        if (elementManifestPredicate is null)
            elementManifest = element.Manifests[0];
        else
            elementManifest = element.Manifests.Find(elementManifestPredicate) ?? throw new InvalidOperationException("Could not find ManifestInfoElement based on predicate");

        string? cachePath = null;

        if (options.ManifestCacheDirectory is not null)
        {
            cachePath = Path.Join(options.ManifestCacheDirectory.AsSpan(), GetFileName(elementManifest.Uri));
            if (File.Exists(cachePath))
            {
                var manifestBuffer = await File.ReadAllBytesAsync(cachePath, cancellationToken).ConfigureAwait(false);
                var manifest = FBuildPatchAppManifest.Deserialize(manifestBuffer, options);
                return (manifest, element);
            }

            static ReadOnlySpan<char> GetFileName(Uri uri)
            {
                var span = uri.OriginalString.AsSpan();
                return span[(span.LastIndexOf('/') + 1)..];
            }
        }

        {
            Uri manifestUri;

            if (elementManifest.QueryParams is { Count: not 0 })
            {
                var url = new Url(elementManifest.Uri);
                foreach (var queryParam in elementManifest.QueryParams)
                {
                    url.AppendQueryParam(queryParam.Name, queryParam.Value, true, NullValueHandling.NameOnly);
                }
                manifestUri = url.ToUri();
            }
            else
            {
                manifestUri = elementManifest.Uri;
            }

            options.CreateDefaultClient();
            byte[] manifestBuffer;

            try
            {
                manifestBuffer = await options.Client!.GetByteArrayAsync(manifestUri, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException httpEx)
            {
                httpEx.Data.Add("ManifestUri", manifestUri);
                httpEx.Data.Add("ElementManifest", elementManifest);
                httpEx.Data.Add("Element", element);
                throw;
            }

            var manifest = FBuildPatchAppManifest.Deserialize(manifestBuffer, options);

            if (cachePath is not null)
            {
                await File.WriteAllBytesAsync(cachePath, manifestBuffer, cancellationToken).ConfigureAwait(false);
            }

            return (manifest, element);
        }
    }
}

/// <summary/>
public class ManifestInfoElement
{
    /// <summary/>
    public required string AppName { get; set; }
    /// <summary/>
    public required string LabelName { get; set; }
    /// <summary/>
    public required string BuildVersion { get; set; }
    /// <summary/>
    public FSHAHash Hash { get; set; }
    /// <summary/>
    public bool UseSignedUrl { get; set; }
    /// <summary/>
    public Dictionary<string, object>? Metadata { get; set; }
    /// <summary/>
    public required List<ManifestInfoElementManifest> Manifests { get; set; }

    /// <inheritdoc cref="ManifestExtensions.TryParseVersionAndCL"/>
    public bool TryParseVersionAndCL([NotNullWhen(true)] out Version? version, out int cl) =>
        ManifestExtensions.TryParseVersionAndCL(BuildVersion, out version, out cl);
}

/// <summary/>
public class ManifestInfoElementManifest
{
    /// <summary/>
    public required Uri Uri { get; set; }
    /// <summary/>
    public List<ManifestInfoElementManifestQueryParams>? QueryParams { get; set; }
}

/// <summary/>
public class ManifestInfoElementManifestQueryParams
{
    /// <summary/>
    public required string Name { get; set; }
    /// <summary/>
    public required string Value { get; set; }
}


/// <summary>
/// Source generated JSON parsers for <see cref="ManifestInfo"/>
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, Converters = [typeof(FSHAHashConverter)])]
[JsonSerializable(typeof(ManifestInfo))]
public partial class EpicManifestParserJsonContext : JsonSerializerContext;


/// <summary>
/// Extension methods for manifest related things
/// </summary>
public static partial class ManifestExtensions
{
    [GeneratedRegex(@"(\d+(?:\.\d+)+)-CL-(\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    internal static partial Regex VersionAndClRegex();

    /// <summary>
    /// Attempts to parse <paramref name="version"/> and <paramref name="cl"/> from the <paramref name="buildVersion"/>.
    /// </summary>
    /// <param name="buildVersion"></param>
    /// <param name="version"></param>
    /// <param name="cl"></param>
    /// <returns><see langword="true"/> if <paramref name="buildVersion" /> was successfully parsed; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseVersionAndCL(string buildVersion, [NotNullWhen(true)] out Version? version, out int cl)
    {
        version = null;
        cl = -1;
        if (string.IsNullOrEmpty(buildVersion))
            return false;

        var match = VersionAndClRegex().Match(buildVersion);
        if (!match.Success)
            return false;

        version = Version.Parse(match.Groups[1].ValueSpan);
        cl = int.Parse(match.Groups[2].ValueSpan);
        return true;
    }

    /// <summary>
    /// Reads the HTTP content and returns the value that results from deserializing the content as JSON in an asynchronous operation.
    /// </summary>
    /// <param name="content">The content to read from.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public static Task<ManifestInfo?> ReadManifestInfoAsync(this HttpContent content, CancellationToken cancellationToken = default)
        => content.ReadFromJsonAsync(EpicManifestParserJsonContext.Default.ManifestInfo, cancellationToken);
}
