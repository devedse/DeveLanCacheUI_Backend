using DeveLanCacheUI_Backend.SteamProto;

namespace DeveLanCacheUI_Backend.Tests.ProtoTest
{
    [TestClass]
    public class SuperProtoTest
    {
        [TestMethod]
        public void GoTest()
        {
            var path = Path.Combine("ProtoTest", "10336669592858206477");
            var allBytes = File.ReadAllBytes(path);

            var dbSteamManifest = SteamManifestHelper.ManifestBytesToDbSteamManifest(allBytes);

            Assert.IsNotNull(dbSteamManifest);
        }
    }
}
