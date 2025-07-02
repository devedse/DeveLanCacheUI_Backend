namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

/// <summary>
/// UE FChunkPart struct
/// </summary>
public readonly struct FChunkPart
{
    /// <summary>
    /// The GUID of the chunk containing this part.
    /// </summary>
    public FGuid Guid { get; }
    /// <summary>
    /// The offset of the first byte into the chunk.
    /// </summary>
    public uint32 Offset { get; }
    /// <summary>
    /// The size of this part.
    /// </summary>
    public uint32 Size { get; }

    internal FChunkPart(FGuid guid, uint32 offset, uint32 size)
    {
        Guid = guid;
        Offset = offset;
        Size = size;
    }

    internal FChunkPart(ref ManifestReader reader)
    {
        var startPos = reader.Position;
        var dataSize = reader.Read<int32>();

        Guid = reader.Read<FGuid>();
        Offset = reader.Read<uint32>();
        Size = reader.Read<uint32>();

        reader.Position = startPos + dataSize;
    }

#if NET9_0_OR_GREATER
    internal static FChunkPart Read(ref ManifestReader reader) => new(ref reader);
#else
	internal static FChunkPart Read(GenericReader.IGenericReader genericReader)
	{
		var reader = (ManifestReader)genericReader;
		return new FChunkPart(ref reader);
	}
#endif
}
