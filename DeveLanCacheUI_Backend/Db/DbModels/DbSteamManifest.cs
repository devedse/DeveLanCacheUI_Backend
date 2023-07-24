using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    [PrimaryKey(nameof(DepotId), nameof(CreationTime))]
    [Index(nameof(UniqueManifestIdentifier), IsUnique = true)]
    public class DbSteamManifest
    {
        public required int DepotId { get; set; }
        public required DateTime CreationTime { get; set; }

        public required ulong TotalCompressedSize { get; set; }
        public required ulong TotalUncompressedSize { get; set; }

        public required string UniqueManifestIdentifier { get; set; }

        public required ulong ManifestBytesSize { get; set; }
    }
}
