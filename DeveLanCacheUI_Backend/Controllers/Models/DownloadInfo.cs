namespace DeveLanCacheUI_Backend.Controllers.Models
{
    public class DownloadInfo
    {
        public string? Name { get; set; }
        
        public string? AppUrl { get; set; }
        public string? AppImageUrl { get; set; }

        //E.g. the DepotId
        public string? DownloadIdentifier { get; set; }
        public string? DownloadIdentifierUrl { get; set; }
        public string? DownloadIdentifierImageUrl { get; set; }

        public ulong? TotalBytes { get; set; }
    }
}
