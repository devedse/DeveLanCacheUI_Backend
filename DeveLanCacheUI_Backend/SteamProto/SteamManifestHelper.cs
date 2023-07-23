using DeveLanCacheUI_Backend.Db.DbModels;
using SteamKit2;
using System.Text.Json;

namespace DeveLanCacheUI_Backend.SteamProto
{
    public static class SteamManifestHelper
    {
        public static DbSteamManifest? ManifestBytesToDbSteamManifest(byte[] manifestBytes, bool storeBytesInDbObject)
        {
            var bytesDecompressed = ZipUtil.Decompress(manifestBytes);
            var depotManifest = DepotManifest.Deserialize(bytesDecompressed);

            if (depotManifest == null)
            {
                return null;
            }

            var dbSteamManifest = new DbSteamManifest()
            {
                DepotId = (int)depotManifest.DepotID,
                CreationTime = depotManifest.CreationTime,
                TotalCompressedSize = depotManifest.TotalCompressedSize,
                TotalUncompressedSize = depotManifest.TotalUncompressedSize,
                ManifestBytesSize = (ulong)manifestBytes.LongLength,
                OriginalProtobufData = storeBytesInDbObject ? manifestBytes : null
            };

            return dbSteamManifest;
        }

        public static string ManifestBytesToJson(byte[] manifestBytes, bool indented)
        {
            var bytesDecompressed = ZipUtil.Decompress(manifestBytes);
            var depotManifest = DepotManifest.Deserialize(bytesDecompressed);
            var json = JsonSerializer.Serialize(depotManifest, new JsonSerializerOptions() { WriteIndented = indented });
            return json;
        }

        public static JsonDocument ManifestBytesToJsonValue(byte[] manifestBytes)
        {
            var json = ManifestBytesToJson(manifestBytes, false);
            var document = JsonDocument.Parse(json);
            return document;
        }

        public static string DecodeBase64(this byte[] data)
        {
            string chunkIDHex = BitConverter.ToString(data).Replace("-", "").ToLower();
            return chunkIDHex;
        }
    }
}
