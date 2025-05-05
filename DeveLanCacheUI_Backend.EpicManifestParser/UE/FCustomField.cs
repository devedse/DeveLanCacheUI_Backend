namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

/// <summary>
/// UE FCustomField struct
/// </summary>
public sealed class FCustomField
{
    /// <summary>
    /// Field name
    /// </summary>
    public string Name { get; internal set; } = "";
    /// <summary>
    /// Field value
    /// </summary>
    public string Value { get; internal set; } = "";

    internal FCustomField() { }

    internal static FCustomField[] ReadCustomFields(ref ManifestReader reader)
    {
        var startPos = reader.Position;
        var dataSize = reader.Read<int32>();
        var dataVersion = reader.Read<EChunkDataListVersion>();
        var elementCount = reader.Read<int32>();

        var fields = new FCustomField[elementCount];
        var fieldsSpan = fields.AsSpan();

        if (dataVersion >= EChunkDataListVersion.Original)
        {
            for (var i = 0; i < elementCount; i++)
            {
                var field = new FCustomField();
                field.Name = reader.ReadFString();
                fieldsSpan[i] = field;
            }
            for (var i = 0; i < elementCount; i++)
                fieldsSpan[i].Value = reader.ReadFString();
        }
        else
        {
            var defaultField = new FCustomField();
            fieldsSpan.Fill(defaultField);
        }

        reader.Position = startPos + dataSize;
        return fields;
    }
}
