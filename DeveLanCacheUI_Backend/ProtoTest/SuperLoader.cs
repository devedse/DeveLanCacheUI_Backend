using ProtoBuf;
using SteamKit2;
using SteamKit2.Internal;

namespace DeveLanCacheUI_Backend.ProtoTest
{
    public static class SuperLoader
    {
        public static void GoLoad(string filePath)
        {
            var allBytes = File.ReadAllBytes(filePath);
            var bytesDecompressed = ZipUtil.Decompress(allBytes);
            using var openBytes = File.OpenRead(filePath);
            var retval = DepotManifest.Deserialize(bytesDecompressed);

            Console.WriteLine(retval);
        }
    }
}
