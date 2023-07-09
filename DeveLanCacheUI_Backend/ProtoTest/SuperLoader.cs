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
            using var openBytes = File.OpenRead(filePath);
            //var retval = DepotManifest.Deserialize(allBytes);
            //var retval = new Steam3Manifest(allBytes);
            var retval = Serializer.Deserialize<ContentManifestPayload>(openBytes);

            Console.WriteLine(retval);
        }
    }
}
