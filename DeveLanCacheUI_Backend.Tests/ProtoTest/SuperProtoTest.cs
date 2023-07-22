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


            SuperLoader.GoLoad(path);

        }
    }
}
