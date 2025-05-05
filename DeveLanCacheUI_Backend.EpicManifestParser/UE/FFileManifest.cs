namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

/// <summary>
/// UE FFileManifest struct
/// </summary>
public sealed class FFileManifest : IComparable<FFileManifest>, IComparable
{
    /// <summary>
    /// The build relative filename.
    /// </summary>
    public string FileName { get; internal set; } = "";
    /// <summary>
    /// Whether this is a symlink to another file.
    /// </summary>
    public string SymlinkTarget { get; internal set; } = "";
    /// <summary>
    /// The file SHA1.
    /// </summary>
    public FSHAHash FileHash { get; internal set; }
    /// <summary>
    /// The flags for this file.
    /// </summary>
    public EFileMetaFlags FileMetaFlags { get; internal set; }
    /// <summary>
    /// The install tags for this file.
    /// </summary>
    public IReadOnlyList<string> InstallTags { get; internal set; } = [];
    /// <summary>
    /// The list of chunk parts to stitch.
    /// </summary>
    public IReadOnlyList<FChunkPart> ChunkParts => ChunkPartsArray;
    internal FChunkPart[] ChunkPartsArray = [];
    /// <summary>
    /// The size of this file.
    /// </summary>
    public int64 FileSize { get; internal set; }
    /// <summary>
    /// The mime type.
    /// </summary>
    public string MimeType { get; internal set; } = "";

    internal FBuildPatchAppManifest Manifest { get; set; } = null!;

    internal FFileManifest() { }

    internal static FFileManifest[] ReadFileDataList(ref ManifestReader reader, FBuildPatchAppManifest manifest)
    {
        var startPos = reader.Position;
        var dataSize = reader.Read<int32>();
        var dataVersion = reader.Read<EFileManifestListVersion>();
        var elementCount = reader.Read<int32>();

        var files = new FFileManifest[elementCount];
        var filesSpan = files.AsSpan();

        if (dataVersion >= EFileManifestListVersion.Original)
        {
            for (var i = 0; i < elementCount; i++)
            {
                var file = new FFileManifest();
                file.FileName = reader.ReadFString();
                filesSpan[i] = file;
            }
            for (var i = 0; i < elementCount; i++)
                filesSpan[i].SymlinkTarget = reader.ReadFString();
            for (var i = 0; i < elementCount; i++)
                filesSpan[i].FileHash = reader.Read<FSHAHash>();
            for (var i = 0; i < elementCount; i++)
                filesSpan[i].FileMetaFlags = reader.Read<EFileMetaFlags>();
            for (var i = 0; i < elementCount; i++)
                filesSpan[i].InstallTags = reader.ReadFStringArray();
            for (var i = 0; i < elementCount; i++)
                filesSpan[i].ChunkPartsArray = reader.ReadArray(FChunkPart.Read);

            // not to be found in UE, maybe fn specific?
            if (dataVersion >= (EFileManifestListVersion)2)
            {
                for (var i = 0; i < elementCount; i++) // TArray<Unknown>
                {
                    var a = reader.Read<int32>();
                    reader.Position += a * 16;
                }
                for (var i = 0; i < elementCount; i++)
                    filesSpan[i]!.MimeType = reader.ReadFString();
                for (var i = 0; i < elementCount; i++) // Unknown
                    reader.Position += 32;
            }

            // FileDataList.OnPostLoad();
            {
                Array.Sort(files);
                for (var i = 0; i < elementCount; i++)
                {
                    var file = filesSpan[i];
                    file.Manifest = manifest;
                    foreach (var chunkPart in file.ChunkPartsArray.AsSpan())
                    {
                        file.FileSize += chunkPart.Size;
                    }
                }
            }
        }
        else
        {
            var defaultFile = new FFileManifest();
            filesSpan.Fill(defaultFile);
        }

        reader.Position = startPos + dataSize;
        return files;
    }

    /// <summary>
    /// Creates a read-only stream to read filedata from.
    /// </summary>
    public FFileManifestStream GetStream() => new(this, Manifest.Options.CacheChunksAsIs);

    /// <summary>
    /// Creates a read-only stream to read filedata from.
    /// </summary>
    /// <param name="cacheAsIs">Whether or not to cache the chunks 1:1 as they were downloaded.</param>
    public FFileManifestStream GetStream(bool cacheAsIs) => new(this, cacheAsIs);


    /// <inheritdoc />
    public int CompareTo(FFileManifest? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return string.Compare(FileName, other.FileName, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is FFileManifest other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(FFileManifest)}");
    }

    /// <summary/>
    public static bool operator <(FFileManifest? left, FFileManifest? right)
    {
        return Comparer<FFileManifest>.Default.Compare(left, right) < 0;
    }

    /// <summary/>
    public static bool operator >(FFileManifest? left, FFileManifest? right)
    {
        return Comparer<FFileManifest>.Default.Compare(left, right) > 0;
    }

    /// <summary/>
    public static bool operator <=(FFileManifest? left, FFileManifest? right)
    {
        return Comparer<FFileManifest>.Default.Compare(left, right) <= 0;
    }

    /// <summary/>
    public static bool operator >=(FFileManifest? left, FFileManifest? right)
    {
        return Comparer<FFileManifest>.Default.Compare(left, right) >= 0;
    }
}
