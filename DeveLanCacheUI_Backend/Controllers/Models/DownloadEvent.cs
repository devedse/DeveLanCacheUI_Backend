namespace DeveLanCacheUI_Backend.Controllers.Models
{
    public class DownloadEvent
    {
        public required int Id { get; set; }

        //steam/epicgames/wsus/epicgames
        public required string CacheIdentifier { get; set; }

        public uint? DownloadIdentifier { get; set; }
        public string? DownloadIdentifierString { get; set; }

        public required string ClientIp { get; set; }

        public required DateTime CreatedAt { get; set; }
        public required DateTime LastUpdatedAt { get; set; }

        public long CacheHitBytes { get; set; }
        public long CacheMissBytes { get; set; }

        public ulong TotalBytes { get; set; }


        public SteamDepot? SteamDepot { get; set; }
    }
}
