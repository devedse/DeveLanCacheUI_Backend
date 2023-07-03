using System.ComponentModel.DataAnnotations;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSteamAppDownloadEvent
    {
        [Key]
        public int Id { get; set; }

        public int SteamDepotId { get; set; }
        public DbSteamDepot SteamDepot { get; set; } = null!;

        public required string ClientIp { get; set; }

        public required DateTime CreatedAt { get; set; }
        public required DateTime LastUpdatedAt { get; set; }

        public long CacheHitBytes { get; set; }
        public long CacheMissBytes { get; set; }
    }
}
