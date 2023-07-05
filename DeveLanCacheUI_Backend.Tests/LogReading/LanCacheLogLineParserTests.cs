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

            Assert.AreEqual("steam", splitted[0]);
            Assert.AreEqual("10.88.10.1", splitted[1]);
            Assert.AreEqual("/", splitted[2]);
            Assert.AreEqual("-", splitted[3]);
            Assert.AreEqual("-", splitted[4]);
            Assert.AreEqual("-", splitted[5]);
            Assert.AreEqual("28/Jun/2023:20:14:49 +0200", splitted[6]);
            Assert.AreEqual("GET /depot/434174/chunk/9437c354e87778aeafe94a65ee042432440d4037 HTTP/1.1", splitted[7]);
            Assert.AreEqual("200", splitted[8]);
            Assert.AreEqual("392304", splitted[9]);
            Assert.AreEqual("-", splitted[10]);
            Assert.AreEqual("Valve/Steam HTTP Client 1.0", splitted[11]);
            Assert.AreEqual("HIT", splitted[12]);
            Assert.AreEqual("cache1-ams1.steamcontent.com", splitted[13]);
            Assert.AreEqual("-", splitted[14]);
        }

        [TestMethod]
        public void SplittedToLanCacheLogEntryRaw_ParseCorrectly1()
        {
            var splitted = LanCacheLogLineParser.SuperSplit("[steam] 10.88.10.1 / - - - [28/Jun/2023:20:14:49 +0200] \"GET /depot/434174/chunk/9437c354e87778aeafe94a65ee042432440d4037 HTTP/1.1\" 200 392304 \"-\" \"Valve/Steam HTTP Client 1.0\" \"HIT\" \"cache1-ams1.steamcontent.com\" \"-\"");
            var logEntry = LanCacheLogLineParser.SplittedToLanCacheLogEntryRaw(splitted);

            Assert.AreEqual("steam", logEntry.CacheIdentifier);
            Assert.AreEqual("10.88.10.1", logEntry.RemoteAddress);
            Assert.AreEqual("-", logEntry.ForwardedFor);
            Assert.AreEqual("-", logEntry.RemoteUser);
            Assert.AreEqual("28/Jun/2023:20:14:49 +0200", logEntry.TimeLocal);
            Assert.AreEqual("GET /depot/434174/chunk/9437c354e87778aeafe94a65ee042432440d4037 HTTP/1.1", logEntry.Request);
            Assert.AreEqual("200", logEntry.Status);
            Assert.AreEqual("392304", logEntry.BodyBytesSent);
            Assert.AreEqual("-", logEntry.Referer);
            Assert.AreEqual("Valve/Steam HTTP Client 1.0", logEntry.UserAgent);
            Assert.AreEqual("HIT", logEntry.UpstreamCacheStatus);
            Assert.AreEqual("cache1-ams1.steamcontent.com", logEntry.Host);
            Assert.AreEqual("-", logEntry.HttpRange);
        }

        [TestMethod]
        public void SuperSplit_ParseCorrectly2()
        {
            var splitted = LanCacheLogLineParser.SuperSplit("[127.0.0.1] 127.0.0.1 / - - - [01/Jul/2023:03:32:18 +0200] \"GET /lancache-heartbeat HTTP/1.1\" 204 0 \"-\" \"Wget/1.19.4 (linux-gnu)\" \"-\" \"127.0.0.1\" \"-\"");

            Assert.AreEqual("127.0.0.1", splitted[0]);
            Assert.AreEqual("127.0.0.1", splitted[1]);
            Assert.AreEqual("/", splitted[2]);
            Assert.AreEqual("-", splitted[3]);
            Assert.AreEqual("-", splitted[4]);
            Assert.AreEqual("-", splitted[5]);
            Assert.AreEqual("01/Jul/2023:03:32:18 +0200", splitted[6]);
            Assert.AreEqual("GET /lancache-heartbeat HTTP/1.1", splitted[7]);
            Assert.AreEqual("204", splitted[8]);
            Assert.AreEqual("0", splitted[9]);
            Assert.AreEqual("-", splitted[10]);
            Assert.AreEqual("Wget/1.19.4 (linux-gnu)", splitted[11]);
            Assert.AreEqual("-", splitted[12]);
            Assert.AreEqual("127.0.0.1", splitted[13]);
            Assert.AreEqual("-", splitted[14]);
        }

        [TestMethod]
        public void SuperSplit_ParseCorrectly3()
        {
            var splitted = LanCacheLogLineParser.SuperSplit("[wsus] 10.88.40.87 / - - - [04/Jul/2023:00:52:23 +0200] \"GET /msdownload/update/v3/static/trustedr/en/disallowedcertstl.cab?45cd1ac7a6214efe HTTP/1.1\" 304 0 \"-\" \"Microsoft-CryptoAPI/10.0\" \"-\" \"ctldl.windowsupdate.com\" \"-\"");

            Assert.AreEqual("wsus", splitted[0]);
            Assert.AreEqual("10.88.40.87", splitted[1]);
            Assert.AreEqual("/", splitted[2]);
            Assert.AreEqual("-", splitted[3]);
            Assert.AreEqual("-", splitted[4]);
            Assert.AreEqual("-", splitted[5]);
            Assert.AreEqual("04/Jul/2023:00:52:23 +0200", splitted[6]);
            Assert.AreEqual("GET /msdownload/update/v3/static/trustedr/en/disallowedcertstl.cab?45cd1ac7a6214efe HTTP/1.1", splitted[7]);
            Assert.AreEqual("304", splitted[8]);
            Assert.AreEqual("0", splitted[9]);
            Assert.AreEqual("-", splitted[10]);
            Assert.AreEqual("Microsoft-CryptoAPI/10.0", splitted[11]);
            Assert.AreEqual("-", splitted[12]);
            Assert.AreEqual("ctldl.windowsupdate.com", splitted[13]);
            Assert.AreEqual("-", splitted[14]);
        }
    }
}
