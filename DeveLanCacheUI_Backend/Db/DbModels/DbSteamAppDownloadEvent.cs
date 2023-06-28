using System.ComponentModel.DataAnnotations;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSteamAppDownloadEvent
    {
        [Key]
        public int Id { get; set; }

        public int SteamAppId { get; set; }
        public DbSteamApp SteamApp { get; set; }

        public string ClientIp { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        public long CacheHitBytes { get; set; }
        public long CacheMissBytes { get; set; }
    }
}
