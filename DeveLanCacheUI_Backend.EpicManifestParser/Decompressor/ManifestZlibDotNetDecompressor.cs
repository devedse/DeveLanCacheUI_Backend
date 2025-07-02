using System.IO.Compression;

namespace DeveLanCacheUI_Backend.EpicManifestParser.Decompressor;

/// <summary>
/// A decompressor using <see cref="Zlibng"/>.
/// </summary>
public static class ManifestZlibDotNetDecompressor
{
    ///// <summary>
    ///// Decompresses data buffer into destination buffer.
    ///// </summary>
    ///// <returns><see langword="true"/> if the decompression was successful; otherwise, <see langword="false"/>.</returns>
    //public static bool Decompress(object? state, byte[] source, int sourceOffset, int sourceLength, byte[] destination, int destinationOffset, int destinationLength)
    //{
    //	var zlibng = (Zlibng)state!;

    //	var result = zlibng.Uncompress(destination.AsSpan(destinationOffset, destinationLength),
    //		source.AsSpan(sourceOffset, sourceLength), out int bytesWritten);

    //	return result == ZlibngCompressionResult.Ok && bytesWritten == destinationLength;
    //}

    /// <summary>
    /// Decompresses <paramref name="source"/> into <paramref name="destination"/>.
    /// </summary>
    /// <remarks>
    /// • The <paramref name="state"/> parameter is kept only to preserve the  
    ///   original delegate/signature; it is not used here.<br/>
    /// • Works on .NET 6+ with <c>ZLibStream</c>.  
    ///   For earlier versions the code falls back to <c>DeflateStream</c>.
    /// </remarks>
    /// <returns>
    /// <c>true</c> when exactly <paramref name="destinationLength"/> bytes were
    /// written; otherwise <c>false</c>.
    /// </returns>
    public static bool Decompress(
        object? state,
        byte[] source,
        int sourceOffset,
        int sourceLength,
        byte[] destination,
        int destinationOffset,
        int destinationLength)
    {
        // Wrap the compressed segment in a read-only MemoryStream.
        using var input = new MemoryStream(source, sourceOffset, sourceLength, writable: false);

#if NET6_0_OR_GREATER
        // ZLibStream understands the standard zlib header/footer.
        using var decompressor = new ZLibStream(input, CompressionMode.Decompress, leaveOpen: false);
#else
        // Older runtimes: fall back to raw deflate (no zlib header).
        using var decompressor = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: false);
#endif

        int totalRead = 0;
        while (totalRead < destinationLength)
        {
            int read = decompressor.Read(
                destination,
                destinationOffset + totalRead,
                destinationLength - totalRead);

            if (read == 0)
                break; // Stream ended prematurely.

            totalRead += read;
        }

        return totalRead == destinationLength;
    }
}
