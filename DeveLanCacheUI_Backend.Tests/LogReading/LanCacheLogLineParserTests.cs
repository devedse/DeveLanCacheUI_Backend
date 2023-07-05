using DeveLanCacheUI_Backend.LogReading;

namespace DeveLanCacheUI_Backend.Tests.LogReading
{
    [TestClass]
    public class LanCacheLogLineParserTests
    {
        [TestMethod]
        public void SuperSplit_ParseCorrectly1()
        {
            var splitted = LanCacheLogLineParser.SuperSplit("[steam] 10.88.10.1 / - - - [28/Jun/2023:20:14:49 +0200] \"GET /depot/434174/chunk/9437c354e87778aeafe94a65ee042432440d4037 HTTP/1.1\" 200 392304 \"-\" \"Valve/Steam HTTP Client 1.0\" \"HIT\" \"cache1-ams1.steamcontent.com\" \"-\"");

            Assert.AreEqual("[steam]", splitted[0]);
            Assert.AreEqual("10.88.10.1", splitted[1]);
            Assert.AreEqual("/", splitted[2]);
            Assert.AreEqual("-", splitted[3]);
            Assert.AreEqual("-", splitted[4]);
            Assert.AreEqual("-", splitted[5]);
            Assert.AreEqual("[28/Jun/2023:20:14:49", splitted[6]);
            Assert.AreEqual("+0200]", splitted[7]);
            Assert.AreEqual("\"GET", splitted[8]);
            Assert.AreEqual("/depot/434174/chunk/9437c354e87778aeafe94a65ee042432440d4037", splitted[9]);
            Assert.AreEqual("HTTP/1.1\"", splitted[10]);
            Assert.AreEqual("200", splitted[11]);
            Assert.AreEqual("392304", splitted[12]);
            Assert.AreEqual("\"-\"", splitted[13]);
            Assert.AreEqual("\"Valve/Steam", splitted[14]);
            Assert.AreEqual("HTTP", splitted[15]);
            Assert.AreEqual("Client", splitted[16]);
            Assert.AreEqual("1.0\"", splitted[17]);
            Assert.AreEqual("\"HIT\"", splitted[18]);
            Assert.AreEqual("\"cache1-ams1.steamcontent.com\"", splitted[19]);
            Assert.AreEqual("\"-\"", splitted[20]);
        }
    }
}
