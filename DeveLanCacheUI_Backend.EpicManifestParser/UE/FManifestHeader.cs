namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

internal class FManifestHeader
{
    public const uint32 Magic = 0x44BEC00C;

    /// <summary>
    /// The version of this header and manifest data format, driven by the feature level.
    /// </summary>
    public readonly EFeatureLevel Version;
    /// <summary>
    /// The size of this header.
    /// </summary>
    public readonly int32 HeaderSize;
    /// <summary>
    /// The size of this data compressed.
    /// </summary>
    public readonly int32 DataSizeCompressed;
    /// <summary>
    /// The size of this data uncompressed.
    /// </summary>
    public readonly int32 DataSizeUncompressed;
    /// <summary>
    /// How the chunk data is stored.
    /// </summary>
    public readonly EManifestStorageFlags StoredAs;
    /// <summary>
    /// The SHA1 hash for the manifest data that follows.
    /// </summary>
    public readonly FSHAHash SHAHash;

    internal FManifestHeader(ref ManifestReader reader)
    {
        var magic = reader.Read<uint32>();
        if (magic != Magic)
            throw new FileLoadException($"Invalid manifest header magic: 0x{magic:X}");

        HeaderSize = reader.Read<int32>();
        DataSizeUncompressed = reader.Read<int32>();
        DataSizeCompressed = reader.Read<int32>();
        SHAHash = reader.Read<FSHAHash>();
        StoredAs = reader.Read<EManifestStorageFlags>();
        Version = HeaderSize > ManifestHeaderVersionSizes[(int32)EFeatureLevel.Original]
            ? reader.Read<EFeatureLevel>()
            : EFeatureLevel.StoredAsCompressedUClass;

        reader.SetPosition(HeaderSize);
    }

    // The constant minimum sizes for each version of a header struct. Must be updated.
    // If new member variables are added the version MUST be bumped and handled properly here,
    // and these values must never change.
    private static readonly uint32[] ManifestHeaderVersionSizes =
    [
		// EFeatureLevel::Original is 37B (32b Magic, 32b HeaderSize, 32b DataSizeUncompressed, 32b DataSizeCompressed, 160b SHA1, 8b StoredAs)
		// This remained the same all up to including EFeatureLevel::StoresPrerequisiteIds.
		37, 37, 37, 37, 37, 37, 37, 37, 37, 37, 37, 37, 37, 37,
		// EFeatureLevel::StoredAsBinaryData is 41B, (296b Original, 32b Version).
		// This remained the same all up to including EFeatureLevel::UsesBuildTimeGeneratedBuildId.
		41, 41, 41, 41, 41,
		// Undocumented, but the latest version is still 41B
		41, 41, 41
    ];
}
