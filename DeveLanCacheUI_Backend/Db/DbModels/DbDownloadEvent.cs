using System.ComponentModel.DataAnnotations;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbDownloadEvent
    {
        [Key]
        public int Id { get; set; }

        //steam/epicgames/wsus/epicgames
        public required string CacheIdentifier { get; set; }

        public int? DownloadIdentifier { get; set; }
        public string? DownloadIdentifierString { get; set; }

        public required string ClientIp { get; set; }

        public required DateTime CreatedAt { get; set; }
        public required DateTime LastUpdatedAt { get; set; }

        public long CacheHitBytes { get; set; }
        public long CacheMissBytes { get; set; }
    }
}
