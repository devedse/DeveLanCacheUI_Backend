using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Tests.DbContextTests
{
    [TestClass]
    public class DbContextDateTimeTests
    {
        [TestMethod]
        public async Task ConvertsDateTimeToUtcCorrectly()
        {
            // Create a new SQLite connection string for in-memory database
            var connectionString = "Data Source=:memory:";
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<DeveLanCacheUIDbContext>()
                .UseSqlite(connection)
                .Options;

            var dateTimeInLocalTimeZone = new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Local);
            var expectedUtcDateTime = dateTimeInLocalTimeZone.ToUniversalTime();

            // First context: Create database and insert data
            using (var context = new DeveLanCacheUIDbContext(options))
            {
                // Ensure the database is created
                await context.Database.EnsureCreatedAsync();

                var testDownloadEvent = new DbDownloadEvent
                {
                    Id = 1,
                    CreatedAt = dateTimeInLocalTimeZone,
                    ClientIp = "",
                    CacheIdentifier = "",
                    LastUpdatedAt = dateTimeInLocalTimeZone
                };

                context.DownloadEvents.Add(testDownloadEvent);
                await context.SaveChangesAsync();
            }

            // Second context: Verify the data was persisted and converted correctly
            using (var context = new DeveLanCacheUIDbContext(options))
            {
                var savedEvent = await context.DownloadEvents.FindAsync(1);
                Assert.IsNotNull(savedEvent, "Saved event should not be null.");

                Assert.AreEqual(DateTimeKind.Utc, savedEvent.CreatedAt.Kind, "CreatedAt should be in UTC.");
                Assert.AreEqual(expectedUtcDateTime, savedEvent.CreatedAt, "CreatedAt should be converted to UTC.");
                Assert.AreEqual(expectedUtcDateTime, savedEvent.LastUpdatedAt, "LastUpdatedAt should be converted to UTC.");
            }
        }
    }
}
