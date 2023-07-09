using DeveLanCacheUI_Backend.ProtoTest;

namespace DeveLanCacheUI_Backend.Tests.ProtoTest
{
    [TestClass]
    public class SuperProtoTest
    {
        [TestMethod]
        public void GoTest()
        {
            var path = Path.Combine("ProtoTest", "10336669592858206477");

            var bytes = File.ReadAllBytes(path);
            string aaaaBase64 = Convert.ToBase64String(bytes);

            SuperLoader.GoLoad(path);

        }
    }
}
