using DeveLanCacheUI_Backend.LogReading;
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
            var content = string.Join("\n", Enumerable.Repeat(line, 11)); // 10 such lines
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var _ = sut.TailFrom2(stream, cts.Token).Take(10).ToList(); // Convert to List to execute the entire IEnumerable

            // Assert
            Assert.AreEqual(10 * 200 + 10, sut.TotalBytesRead); // 9 newlines added between 10 lines
        }

        [TestMethod]
        public void TestLineExactly1024_LF()
        {
            // Arrange
            var line = new string('b', 1023); // a line with 1024 characters
            var stream = MockStream(line + "\n");
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var _ = sut.TailFrom2(stream, cts.Token).Take(1).ToList();

            // Assert
            Assert.AreEqual(1024, sut.TotalBytesRead);
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
            var _ = sut.TailFrom2(stream, cts.Token).Take(2).ToList();

            // Assert
            Assert.AreEqual(1023 + 1025 + 2, sut.TotalBytesRead); // +1 for the newline between
        }

        [TestMethod]
        public void TestTenLinesEach200_CRLF()
        {
            // Arrange
            var line = new string('a', 200); // a line with 200 characters
            var content = string.Join("\r\n", Enumerable.Repeat(line, 11)); // 11 lines with \r\n separator
            var stream = MockStream(content);
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var _ = sut.TailFrom2(stream, cts.Token).Take(10).ToList();

            // Assert
            Assert.AreEqual(10 * 200 + 10 * 2, sut.TotalBytesRead); // 10 * 2 for the \r\n between 10 lines
        }

        [TestMethod]
        public void TestLineExactly1024_CRLF()
        {
            // Arrange
            var line = new string('b', 1022); // 1022 characters + 2 for \r\n = 1024
            var stream = MockStream(line + "\r\n");
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var _ = sut.TailFrom2(stream, cts.Token).Take(1).ToList();

            // Assert
            Assert.AreEqual(1024, sut.TotalBytesRead);
        }

        [TestMethod]
        public void TestLineExactly1025_CRLF()
        {
            // Arrange
            var line = new string('b', 1023); // 1022 characters + 2 for \r\n = 1024
            var stream = MockStream(line + "\r\n");
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var _ = sut.TailFrom2(stream, cts.Token).Take(1).ToList();

            // Assert
            Assert.AreEqual(1025, sut.TotalBytesRead);
        }

        [TestMethod]
        public void TestLineExactly1026_CRLF()
        {
            // Arrange
            var line = new string('b', 1024); // 1022 characters + 2 for \r\n = 1024
            var stream = MockStream(line + "\r\n");
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!, null!);
            var cts = new CancellationTokenSource();

            // Act
            var _ = sut.TailFrom2(stream, cts.Token).Take(1).ToList();

            // Assert
            Assert.AreEqual(1026, sut.TotalBytesRead);
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
            Assert.AreEqual(1023 + 1025 + 4, sut.TotalBytesRead); // +4 for the two \r\n sequences
            Assert.AreEqual(1023, data[0].Length);
            Assert.AreEqual(1025, data[0].Length);
        }
    }
}
