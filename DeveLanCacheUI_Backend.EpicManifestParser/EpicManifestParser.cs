using DeveLanCacheUI_Backend.EpicManifestParser.Decompressor;
using DeveLanCacheUI_Backend.EpicManifestParser.UE;

namespace DeveLanCacheUI_Backend.EpicManifestParser
{
    public class EpicManifestParser
    {
        public static FBuildPatchAppManifest Deserialize(ManifestRoData manifestBuffer)
        {
            var options = new ManifestParseOptions
            {
                //ChunkBaseUrl = "http://download.epicgames.com/Builds/UnrealEngineLauncher/CloudDir/",
                //ChunkCacheDirectory = Directory.CreateDirectory(Path.Combine(Benchmarks.DownloadsDir, "chunks_v2")).FullName,
                //ManifestCacheDirectory = Directory.CreateDirectory(Path.Combine(Benchmarks.DownloadsDir, "manifests_v2")).FullName,
            };

            options.Decompressor = ManifestZlibDotNetDecompressor.Decompress;

            var manifest = FBuildPatchAppManifest.Deserialize(manifestBuffer, options);
            return manifest;
        }
    }
}
