namespace DeveLanCacheUI_Backend.Db.DbModels
{
    [PrimaryKey(nameof(DownloadIdentifier), nameof(CreationTime))]
    public class DbEpicManifest
    {
        public required string DownloadIdentifier { get; set; }
        public required DateTime CreationTime { get; set; }

        public required ulong TotalCompressedSize { get; set; }
        public required ulong TotalUncompressedSize { get; set; }

        public required ulong ManifestBytesSize { get; set; }

        public string Name { get; set; }
    }
}
