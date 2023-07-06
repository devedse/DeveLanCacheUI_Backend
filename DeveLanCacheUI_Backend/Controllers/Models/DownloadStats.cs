namespace DeveLanCacheUI_Backend.Controllers.Models
{
    public class DownloadStats
    {
        public required string Identifier { get; set; }
        public long TotalCacheHitBytes { get; set; }
        public long TotalCacheMissBytes { get; set; }
    }
}
