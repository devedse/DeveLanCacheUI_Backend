using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeveLanCacheUI_Backend.EpicManifestParser.Json;

[DebuggerDisplay("Value,nq")]
internal readonly struct BlobString<T> where T : struct
{
    public T Value { get; }
    public BlobString(T value) => Value = value;

    public static BlobString<T>? Parse(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0) return null;
        T result = default;
        var dest = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref result), Unsafe.SizeOf<T>());

        // Make sure the buffer is at least half the size and that the string is an
        // even number of characters long
        if (dest.Length >= (uint32)(source.Length / 3) && source.Length % 3 == 0)
        {
            Span<uint8> convBuffer = stackalloc uint8[4];
            convBuffer[3] = 0;

            int32 WriteIndex = 0;
            // Walk the string 3 chars at a time
            for (int32 Index = 0; Index < source.Length; Index += 3, WriteIndex++)
            {
                convBuffer[0] = source[Index];
                convBuffer[1] = source[Index + 1];
                convBuffer[2] = source[Index + 2];
                dest[WriteIndex] = uint8.Parse(convBuffer);
            }
            return result;
        }
        return null;
    }

    public static implicit operator BlobString<T>(T value)
    {
        return new BlobString<T>(value);
    }

    public static explicit operator T?(BlobString<T>? holder)
    {
        return holder?.Value;
    }
}

internal sealed class BlobStringConverter<T> : JsonConverter<BlobString<T>?> where T : struct
{
    public override BlobString<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => BlobString<T>.Parse(reader.ValueSpan);
    public override void Write(Utf8JsonWriter writer, BlobString<T>? value, JsonSerializerOptions options)
        => throw new NotSupportedException();
}
