using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeveLanCacheUI_Backend.EpicManifestParser.Tests
{
    [TestClass]
    public sealed class EpicManifestParserTests
    {
        [TestMethod]
        public async Task DoesItWork()
        {
            // Arrange
            var manifestBuffer = await File.ReadAllBytesAsync(Path.Combine("TestFiles", "1sE9O19OT_X-rOWTFEiMUGNBYu8I1A.manifest"));

            // Act
            var manifest = EpicManifestParser.Deserialize(manifestBuffer);

            // Assert
            Assert.AreEqual("Super Space Club.exe", manifest.Meta.LaunchExe);
        }
    }
}
