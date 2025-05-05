using System.IO.Compression;

namespace DeveLanCacheUI_Backend.EpicManifestParser;

/// <summary>
/// The default decompressor using <see cref="ZLibStream"/>.
/// </summary>
public static class ManifestZlibStreamDecompressor
{
    /// <summary>
    /// Decompresses data buffer into destination buffer.
    /// </summary>
    /// <returns><see langword="true"/> if the decompression was successful; otherwise, <see langword="false"/>.</returns>
    public static bool Decompress(object? state, byte[] source, int sourceOffset, int sourceLength, byte[] destination, int destinationOffset, int destinationLength)
    {
        using var destinationMs = new MemoryStream(destination, destinationOffset, destinationLength, true, true);
        using var sourceMs = new MemoryStream(source, sourceOffset, sourceLength, false, true);
        using var zlibStream = new ZLibStream(sourceMs, CompressionMode.Decompress);
        zlibStream.CopyTo(destinationMs);
        return destinationMs.Position == destinationLength;
    }
}
