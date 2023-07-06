using System.ComponentModel.DataAnnotations;

namespace DeveLanCacheUI_Backend.Controllers.Models
{
    public class DownloadEvent
    {
        public int Id { get; set; }

        //steam/epicgames/wsus/epicgames
        public string CacheIdentifier { get; set; }

        public int? DownloadIdentifier { get; set; }
        public string? DownloadIdentifierString { get; set; }

        public required string ClientIp { get; set; }

        public required DateTime CreatedAt { get; set; }
        public required DateTime LastUpdatedAt { get; set; }

        public long CacheHitBytes { get; set; }
        public long CacheMissBytes { get; set; }


        public SteamDepot? SteamDepot { get; set; }
    }
}
