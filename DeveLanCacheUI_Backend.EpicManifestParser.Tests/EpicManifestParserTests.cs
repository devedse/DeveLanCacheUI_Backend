using DeveLanCacheUI_Backend.EpicManifestParser.Decompressor;
using DeveLanCacheUI_Backend.EpicManifestParser.UE;

namespace DeveLanCacheUI_Backend.EpicManifestParser.Tests
{
    [TestClass]
    public sealed class EpicManifestParser
    {
        [TestMethod]
        public async Task DoesItWork()
        {
            // Arrange
            var options = new ManifestParseOptions
            {
                //ChunkBaseUrl = "http://download.epicgames.com/Builds/UnrealEngineLauncher/CloudDir/",
                //ChunkCacheDirectory = Directory.CreateDirectory(Path.Combine(Benchmarks.DownloadsDir, "chunks_v2")).FullName,
                //ManifestCacheDirectory = Directory.CreateDirectory(Path.Combine(Benchmarks.DownloadsDir, "manifests_v2")).FullName,
            };

            options.Decompressor = ManifestZlibDotNetDecompressor.Decompress;


            // Act
            var manifestBuffer = await File.ReadAllBytesAsync(Path.Combine("TestFiles", "1sE9O19OT_X-rOWTFEiMUGNBYu8I1A.manifest"));
            var manifest = FBuildPatchAppManifest.Deserialize(manifestBuffer, options);

            // Assert
            Assert.AreEqual("Super Space Club.exe", manifest.Meta.LaunchExe);
        }
    }
}
