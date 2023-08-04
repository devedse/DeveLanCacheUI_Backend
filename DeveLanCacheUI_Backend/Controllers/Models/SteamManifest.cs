namespace DeveLanCacheUI_Backend.Controllers.Models
{
    public class SteamManifest
    {
        public required int DepotId { get; set; }
        public required DateTime CreationTime { get; set; }

        public required ulong TotalCompressedSize { get; set; }
        public required ulong TotalUncompressedSize { get; set; }

        public required JsonDocument? ProtobufDataAsJson { get; set; }

        public required string UniqueManifestIdentifier { get; set; }

        public ulong ManifestBytesSize { get; set; }
    }
}
