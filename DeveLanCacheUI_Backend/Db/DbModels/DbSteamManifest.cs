using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    [PrimaryKey(nameof(DepotId), nameof(DateTime))]
    public class DbSteamManifest
    {
        public required uint DepotId { get; set; }
        public required DateTime CreationTime { get; set; }

        public required ulong TotalCompressedSize { get; set; }
        public required ulong TotalUncompressedSize { get; set; }
        public required ulong CalculatedCompressedSize { get; set; }
        public required ulong CalculatedUncompressedSize { get; set; }
    }
}
