namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

[Flags]
internal enum EChunkStorageFlags : uint8
{
    None = 0x00,

    // Flag for compressed data.
    Compressed = 0x01,

    // Flag for encrypted. If also compressed, decrypt first. Encryption will ruin compressibility.
    Encrypted = 0x02,
}
