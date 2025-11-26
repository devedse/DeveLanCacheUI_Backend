using DeveLanCacheUI_Backend.EpicManifestParser.UE;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeveLanCacheUI_Backend.EpicManifestParser.Json;

internal static class JsonNodeExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters =
        {
            new FGuidConverter(),
            new FSHAHashConverter(),
            new BlobStringConverter<uint8>(),
            new BlobStringConverter<int32>(),
            new BlobStringConverter<uint32>(),
            new BlobStringConverter<int64>(),
            new BlobStringConverter<uint64>(),
            new BlobStringConverter<EFeatureLevel>(),
            new BlobStringConverter<FSHAHash>(),
        }
    };

    public static T GetBlob<T>(this JsonNode? node, T defaultValue = default) where T : struct
    {
        return node.Deserialize<BlobString<T>?>(SerializerOptions)?.Value ?? defaultValue;
    }

    public static FGuid GetFGuid(this JsonNode? node)
    {
        return node.Deserialize<FGuid>(SerializerOptions);
    }

    public static FSHAHash GetSha(this JsonNode? node)
    {
        return node.Deserialize<FSHAHash>(SerializerOptions);
    }

    public static string GetString(this JsonNode? node, string defaultValue = "")
    {
        return node?.GetValue<string>() ?? defaultValue;
    }

    public static T Get<T>(this JsonNode? node, T defaultValue = default!)
    {
        return node is null ? defaultValue : node.GetValue<T>();
    }

    public static T Parse<T>(this JsonNode? node, T defaultValue = default!)
    {
        return node is null ? defaultValue : node.Deserialize<T>() ?? defaultValue;
    }
}
