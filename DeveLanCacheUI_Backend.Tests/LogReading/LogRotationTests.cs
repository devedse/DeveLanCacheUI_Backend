using DeveLanCacheUI_Backend.LogReading;
using System.IO.Compression;
using System.Text;
using ZstdNet;

namespace DeveLanCacheUI_Backend.Tests.LogReading
{
    [TestClass]
    public class LogRotationTests
    {
        private string _tempDirectory = null!;

        [TestInitialize]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [TestMethod]
        public void GetLogFiles_ReturnsCorrectOrderWithBasicFiles()
        {
            // Arrange
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!);
            
            // Create test files
            File.WriteAllText(Path.Combine(_tempDirectory, "access.log"), "current log");
            File.WriteAllText(Path.Combine(_tempDirectory, "access.log.1"), "rotated 1");
            File.WriteAllText(Path.Combine(_tempDirectory, "access.log.2"), "rotated 2");
            
            // Act
            var result = InvokePrivateMethod<List<string>>(sut, "GetLogFiles", _tempDirectory);
            
            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result[0].EndsWith("access.log"));
            Assert.IsTrue(result[1].EndsWith("access.log.1"));
            Assert.IsTrue(result[2].EndsWith("access.log.2"));
        }

        [TestMethod]
        public void GetLogFiles_ReturnsCorrectOrderWithCompressedFiles()
        {
            // Arrange
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!);
            
            // Create test files
            File.WriteAllText(Path.Combine(_tempDirectory, "access.log"), "current log");
            File.WriteAllText(Path.Combine(_tempDirectory, "access.log.1.gz"), "compressed 1");
            File.WriteAllText(Path.Combine(_tempDirectory, "access.log.2.zst"), "compressed 2");
            
            // Act
            var result = InvokePrivateMethod<List<string>>(sut, "GetLogFiles", _tempDirectory);
            
            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result[0].EndsWith("access.log"));
            Assert.IsTrue(result[1].EndsWith("access.log.1.gz"));
            Assert.IsTrue(result[2].EndsWith("access.log.2.zst"));
        }

        [TestMethod]
        public void GetLogFiles_HandlesGapInNumbers()
        {
            // Arrange
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!);
            
            // Create test files with gaps
            File.WriteAllText(Path.Combine(_tempDirectory, "access.log"), "current log");
            File.WriteAllText(Path.Combine(_tempDirectory, "access.log.1"), "rotated 1");
            // Skip access.log.2
            File.WriteAllText(Path.Combine(_tempDirectory, "access.log.3"), "rotated 3");
            
            // Act
            var result = InvokePrivateMethod<List<string>>(sut, "GetLogFiles", _tempDirectory);
            
            // Assert - should stop at the first gap
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result[0].EndsWith("access.log"));
            Assert.IsTrue(result[1].EndsWith("access.log.1"));
        }

        [TestMethod]
        public void OpenLogFileStream_HandlesRegularFile()
        {
            // Arrange
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!);
            var testFile = Path.Combine(_tempDirectory, "test.log");
            var testContent = "test line 1\ntest line 2\n";
            File.WriteAllText(testFile, testContent);
            
            // Act
            using var stream = InvokePrivateMethod<Stream>(sut, "OpenLogFileStream", testFile);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            
            // Assert
            Assert.AreEqual(testContent, content);
        }

        [TestMethod]
        public void OpenLogFileStream_HandlesGzipFile()
        {
            // Arrange
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!);
            var testFile = Path.Combine(_tempDirectory, "test.log.gz");
            var testContent = "test line 1\ntest line 2\n";
            
            // Create gzipped file
            using (var fileStream = File.Create(testFile))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
            using (var writer = new StreamWriter(gzipStream))
            {
                writer.Write(testContent);
            }
            
            // Act
            using var stream = InvokePrivateMethod<Stream>(sut, "OpenLogFileStream", testFile);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            
            // Assert
            Assert.AreEqual(testContent, content);
        }

        [TestMethod]
        public void OpenLogFileStream_HandlesZstdFile()
        {
            // Arrange
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!);
            var testFile = Path.Combine(_tempDirectory, "test.log.zst");
            var testContent = "test line 1\ntest line 2\n";
            
            // Create zstd compressed file
            using (var compressor = new Compressor())
            {
                var originalBytes = Encoding.UTF8.GetBytes(testContent);
                var compressedBytes = compressor.Wrap(originalBytes);
                File.WriteAllBytes(testFile, compressedBytes);
            }
            
            // Act
            using var stream = InvokePrivateMethod<Stream>(sut, "OpenLogFileStream", testFile);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            
            // Assert
            Assert.AreEqual(testContent, content);
        }

        [TestMethod]
        public void ReadAllLinesFromStream_ReadsAllLines()
        {
            // Arrange
            var sut = new LanCacheLogReaderHostedService(null!, null!, null!, null!);
            var testContent = "line1\nline2\nline3\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            var cts = new CancellationTokenSource();
            
            // Act
            var result = InvokePrivateMethod<IEnumerable<string>>(sut, "ReadAllLinesFromStream", stream, cts.Token).ToList();
            
            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("line1", result[0]);
            Assert.AreEqual("line2", result[1]);
            Assert.AreEqual("line3", result[2]);
        }

        /// <summary>
        /// Helper method to invoke private methods for testing
        /// </summary>
        private T InvokePrivateMethod<T>(object obj, string methodName, params object[] parameters)
        {
            var type = obj.GetType();
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null)
                throw new ArgumentException($"Method {methodName} not found");
            
            var result = method.Invoke(obj, parameters);
            return (T)result!;
        }
    }
}