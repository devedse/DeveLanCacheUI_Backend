using DeveLanCacheUI_Backend.SteamProto;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    [PrimaryKey(nameof(DepotId), nameof(CreationTime))]
    public class DbSteamManifest
    {
        public required int DepotId { get; set; }
        public required DateTime CreationTime { get; set; }

        public required ulong TotalCompressedSize { get; set; }
        public required ulong TotalUncompressedSize { get; set; }
        public required ulong CalculatedCompressedSize { get; set; }
        public required ulong CalculatedUncompressedSize { get; set; }

        [JsonIgnore]
        public byte[]? OriginalProtobufData { get; set; }

        [NotMapped]
        //public string ProtobufDataAsJson => OriginalProtobufData != null ? SteamManifestHelper.ManifestBytesToJson(OriginalProtobufData, false) : "";
        public JsonDocument? ProtobufDataAsJson => OriginalProtobufData != null ? SteamManifestHelper.ManifestBytesToJsonValue(OriginalProtobufData) : null;

        public ulong ManifestBytesSize { get; internal set; }
    }
}
