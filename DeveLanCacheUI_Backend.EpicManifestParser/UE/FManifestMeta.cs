namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

/// <summary>
/// UE FManifestMeta struct
/// </summary>
public sealed class FManifestMeta
{
    /// <summary>
    /// The feature level support this build was created with, regardless of the serialised format.
    /// </summary>
    public EFeatureLevel FeatureLevel { get; internal set; } = EFeatureLevel.Invalid;
    /// <summary>
    /// Whether this is a legacy 'nochunks' build.
    /// </summary>
    public bool bIsFileData { get; internal set; }
    /// <summary>
    /// The app id provided at generation.
    /// </summary>
    public uint32 AppID { get; internal set; }
    /// <summary>
    /// The app name string provided at generation.
    /// </summary>
    public string AppName { get; internal set; } = "";
    /// <summary>
    /// The build version string provided at generation.
    /// </summary>
    public string BuildVersion { get; internal set; } = "";
    /// <summary>
    /// The file in this manifest designated the application executable of the build.
    /// </summary>
    public string LaunchExe { get; internal set; } = "";
    /// <summary>
    /// The command line required when launching the application executable.
    /// </summary>
    public string LaunchCommand { get; internal set; } = "";
    /// <summary>
    /// The set of prerequisite ids for dependencies that this build's prerequisite installer will apply.
    /// </summary>
    public string[] PrereqIds { get; internal set; } = [];
    /// <summary>
    /// A display string for the prerequisite provided at generation.
    /// </summary>
    public string PrereqName { get; internal set; } = "";
    /// <summary>
    /// The file in this manifest designated the launch executable of the prerequisite installer.
    /// </summary>
    public string PrereqPath { get; internal set; } = "";
    /// <summary>
    /// The command line required when launching the prerequisite installer.
    /// </summary>
    public string PrereqArgs { get; internal set; } = "";
    /// <summary>
    /// A unique build id generated at original chunking time to identify an exact build.
    /// </summary>
    public string BuildId { get; internal set; } = "";

    /// <summary>
    /// Undocumented
    /// </summary>
    public string UninstallExe { get; internal set; } = "";
    /// <summary>
    /// Undocumented
    /// </summary>
    public string UninstallCommand { get; internal set; } = "";

    internal FManifestMeta() { }
    internal FManifestMeta(ref ManifestReader reader)
    {
        var startPos = reader.Position;
        var dataSize = reader.Read<int32>();
        var dataVersion = reader.Read<EManifestMetaVersion>();

        if (dataVersion >= EManifestMetaVersion.Original)
        {
            FeatureLevel = reader.Read<EFeatureLevel>();
            bIsFileData = reader.Read<uint8>() == 1;
            AppID = reader.Read<uint32>();
            AppName = reader.ReadFString();
            BuildVersion = reader.ReadFString();
            LaunchExe = reader.ReadFString();
            LaunchCommand = reader.ReadFString();
            PrereqIds = reader.ReadFStringArray();
            PrereqName = reader.ReadFString();
            PrereqPath = reader.ReadFString();
            PrereqArgs = reader.ReadFString();
        }

        BuildId = dataVersion >= EManifestMetaVersion.SerialisesBuildId
            ? reader.ReadFString()
            : GetBackwardsCompatibleBuildId(this);

        // not to be found in UE, maybe fn specific?
        if (FeatureLevel > EFeatureLevel.UsesBuildTimeGeneratedBuildId)
        {
            UninstallExe = reader.ReadFString();
            UninstallCommand = reader.ReadFString();
        }

        reader.Position = startPos + dataSize;
    }

    internal static string GetBackwardsCompatibleBuildId(in FManifestMeta meta)
    {
        // TODO: https://github.com/EpicGames/UnrealEngine/blob/a937fa584fbd6d69b7cf9c527907040c9dbf54fc/Engine/Source/Runtime/Online/BuildPatchServices/Private/BuildPatchUtil.cpp#L166
        return "";
    }
}
