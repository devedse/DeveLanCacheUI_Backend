namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

internal enum EFileManifestListVersion : uint8
{
    Original = 0,

    // Always after the latest version, signifies the latest version plus 1 to allow initialization simplicity.
    LatestPlusOne,
    Latest = LatestPlusOne - 1
}
