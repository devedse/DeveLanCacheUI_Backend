using DeveLanCacheUI_Backend.LogReading;
using SteamKit2.Internal;
using System.Text;

namespace DeveLanCacheUI_Backend.Tests.LogReading
{
    [TestClass]
    public class LanCacheLogReaderHostedServiceTests
    {
        private Stream MockStream(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new MemoryStream(bytes);
        }

        [TestMethod]
        public void TestTenLinesEach200_LF()
        {
            // Arrange
            var line = new string('a', 200); // a line with 200 characters
            var content = string.Join("\n", Enumerable.Repeat(line, 11)); // 11 such lines
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(10).ToList();

            // Assert
            Assert.AreEqual(10 * 200 + 10, sut.TotalBytesRead); // 10 newlines added between 10 lines
            data.ForEach(d => Assert.AreEqual(200, d.Length));
        }

        [TestMethod]
        public void TestLineExactly1024_LF()
        {
            // Arrange
            var line = new string('b', 1023);
            var stream = MockStream(line + "\n");
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(1).ToList();

            // Assert
            Assert.AreEqual(1024, sut.TotalBytesRead);
            Assert.AreEqual(1023, data[0].Length);
        }

        [TestMethod]
        public void TestLine1023And1025_LF()
        {
            // Arrange
            var line1 = new string('c', 1023);
            var line2 = new string('d', 1025);
            var content = $"{line1}\n{line2}\n";
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(2).ToList();

            // Assert
            Assert.AreEqual(1023 + 1025 + 2, sut.TotalBytesRead); // +2 for the two '\n' sequences
            Assert.AreEqual(1023, data[0].Length);
            Assert.AreEqual(1025, data[1].Length);
        }

        [TestMethod]
        public void TestTenLinesEach200_CRLF()
        {
            // Arrange
            var line = new string('a', 200);
            var content = string.Join("\r\n", Enumerable.Repeat(line, 11));
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(10).ToList();

            // Assert
            Assert.AreEqual(10 * 200 + 10 * 2, sut.TotalBytesRead);
            data.ForEach(d => Assert.AreEqual(200, d.Length));
        }

        [TestMethod]
        public void TestLineExactly1024_CRLF()
        {
            // Arrange
            var line = new string('b', 1022);
            var stream = MockStream(line + "\r\n");
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(1).ToList();

            // Assert
            Assert.AreEqual(1024, sut.TotalBytesRead);
            Assert.AreEqual(1022, data[0].Length);
        }

        [TestMethod]
        public void TestLineExactly1025_CRLF()
        {
            // Arrange
            var line = new string('b', 1023);
            var stream = MockStream(line + "\r\n");
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(1).ToList();

            // Assert
            Assert.AreEqual(1025, sut.TotalBytesRead);
            Assert.AreEqual(1023, data[0].Length);
        }

        [TestMethod]
        public void TestLineExactly1026_CRLF()
        {
            // Arrange
            var line = new string('b', 1024);
            var stream = MockStream(line + "\r\n");
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(1).ToList();

            // Assert
            Assert.AreEqual(1026, sut.TotalBytesRead);
            Assert.AreEqual(1024, data[0].Length);
        }

        [TestMethod]
        public void TestLine1023And1025_CRLF()
        {
            // Arrange
            var line1 = new string('c', 1023);
            var line2 = new string('d', 1025);
            var content = $"{line1}\r\n{line2}\r\n";
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(2).ToList();

            // Assert
            Assert.AreEqual(1023 + 1025 + 4, sut.TotalBytesRead);
            Assert.AreEqual(1023, data[0].Length);
            Assert.AreEqual(1025, data[1].Length);
        }

        [TestMethod]
        public void When_InitialTotalBytesReadIsSetToSkipFirstLine_ThenShouldOnlyReadLast2Lines_LF()
        {
            // Arrange
            var initialTotalBytesRead = 4;
            var content = "abc\ndef\nghi\n";
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!)
            {
                TotalBytesRead = initialTotalBytesRead
            };
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(2).ToList();

            // Assert
            Assert.AreEqual(2, data.Count);
            Assert.AreEqual("def", data[0]);
            Assert.AreEqual("ghi", data[1]);
            Assert.AreEqual(content.Length, sut.TotalBytesRead);
        }

        [TestMethod]
        public void When_InitialTotalBytesReadIsGreaterThanStreamLength_PositionIsAdjusted_LF()
        {
            // Arrange
            var content = "line1\nline2\nline3\n";
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!)
            {
                TotalBytesRead = 500
            };
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(3).ToList();

            // Assert
            Assert.AreEqual(3, data.Count);
            Assert.AreEqual("line1", data[0]);
            Assert.AreEqual("line2", data[1]);
            Assert.AreEqual("line3", data[2]);
            Assert.AreEqual(content.Length, sut.TotalBytesRead);
        }

        [TestMethod]
        public void When_DoubleNewLine_AtExactBufferLength_Works()
        {
            // Arrange
            var stringOfLength1024 = new string('a', 1023);
            var content = $"{stringOfLength1024}\n\nline2\nline3\n";
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!)
            {
                TotalBytesRead = 0
            };
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(4).ToList();

            // Assert
            Assert.AreEqual(4, data.Count);
            Assert.AreEqual(stringOfLength1024, data[0]);
            Assert.AreEqual("", data[1]);
            Assert.AreEqual("line2", data[2]);
            Assert.AreEqual("line3", data[3]);
            Assert.AreEqual(content.Length, sut.TotalBytesRead);
        }

        [TestMethod]
        public void When_DoubleNewLine_AtExactBufferLength_CRLF_Works()
        {
            // Arrange
            var stringOfLength1024 = new string('a', 1023);
            var content = $"{stringOfLength1024}\n\r\nline2\nline3\n";
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!)
            {
                TotalBytesRead = 0
            };
            var cts = new CancellationTokenSource();

            // Act
            var data = sut.TailFrom2(stream, cts.Token).Take(4).ToList();

            // Assert
            Assert.AreEqual(4, data.Count);
            Assert.AreEqual(stringOfLength1024, data[0]);
            Assert.AreEqual("", data[1]);
            Assert.AreEqual("line2", data[2]);
            Assert.AreEqual("line3", data[3]);
            Assert.AreEqual(content.Length, sut.TotalBytesRead);
        }
    }
}
