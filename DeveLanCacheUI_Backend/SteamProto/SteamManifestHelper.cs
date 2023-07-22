using DeveLanCacheUI_Backend.Db.DbModels;
using ProtoBuf;
using SteamKit2;
using SteamKit2.Internal;
using System.Text.Json;

namespace DeveLanCacheUI_Backend.SteamProto
{
    public static class SteamManifestHelper
    {
        public static DbSteamManifest? ManifestBytesToDbSteamManifest(byte[] manifestBytes)
        {
            var bytesDecompressed = ZipUtil.Decompress(manifestBytes);
            var depotManifest = DepotManifest.Deserialize(bytesDecompressed);

            if (depotManifest == null)
            {
                return null;
            }

            uint totUncompressed = 0;
            uint totCompressed = 0;
            ulong totSize = 0;

            foreach (var file in depotManifest?.Files ?? Enumerable.Empty<DepotManifest.FileData>())
            {
                foreach (var chunk in file.Chunks)
                {
                    var res = chunk.ChunkID?.DecodeBase64();
                    totUncompressed += chunk.UncompressedLength;
                    totCompressed += chunk.CompressedLength;
                }
                totSize += file.TotalSize;
            }

            var dbSteamManifest = new DbSteamManifest()
            {
                DepotId = depotManifest.DepotID,
                CreationTime = depotManifest.CreationTime,
                TotalCompressedSize = depotManifest.TotalCompressedSize,
                TotalUncompressedSize = depotManifest.TotalUncompressedSize,
                CalculatedCompressedSize = totCompressed,
                CalculatedUncompressedSize = totUncompressed
            };

            return dbSteamManifest;
        }

        public static string DecodeBase64(this byte[] data)
        {
            string chunkIDHex = BitConverter.ToString(data).Replace("-", "").ToLower();
            return chunkIDHex;
        }
    }
}
