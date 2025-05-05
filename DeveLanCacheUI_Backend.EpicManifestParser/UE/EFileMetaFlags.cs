namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

/// <summary>
/// UE EFileMetaFlags enum
/// </summary>
[Flags]
public enum EFileMetaFlags : uint8
{
    /// <summary>
    /// None
    /// </summary>
    None = 0,
    /// <summary>
    /// Flag for readonly file.
    /// </summary>
    ReadOnly = 1,
    /// <summary>
    /// Flag for natively compressed.
    /// </summary>
    Compressed = 1 << 1,
    /// <summary>
    /// Flag for unix executable.
    /// </summary>
    UnixExecutable = 1 << 2
}
