using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

/// <summary>
/// UE FGuid struct
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FGuid : IEquatable<FGuid>, ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>
    /// The size of the FGuid/struct.
    /// </summary>
    public const int Size = sizeof(uint32) * 4;

    private readonly uint32 A;
    private readonly uint32 B;
    private readonly uint32 C;
    private readonly uint32 D;

    /// <summary>
    /// Creates a hex string of the guid.
    /// </summary>
    public string GetHexString(bool upperCase = true) => upperCase
        ? $"{A:X8}{B:X8}{C:X8}{D:X8}"
        : $"{A:x8}{B:x8}{C:x8}{D:x8}";

    /// <summary>
    /// Creates a string of the guid.
    /// </summary>
    public string GetGuidString() => $"{A:x8}-{B >> 16:x4}-{B & 0xffff:x4}-{C >> 16:x4}-{C & 0xffff:x4}{D:x8}";

    /// <summary>
    /// Creates a FGuid from values.
    /// </summary>
    /// <param name="a">A value.</param>
    /// <param name="b">B value.</param>
    /// <param name="c">C value.</param>
    /// <param name="d">D value.</param>
    public FGuid(uint32 a, uint32 b, uint32 c, uint32 d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    /// <summary>
    /// Parses a FGuid from a string.
    /// </summary>
    /// <param name="guid">The FGuid string.</param>
    public FGuid(string guid) : this(guid.AsSpan()) { }

    /// <summary>
    /// Parses a FGuid from a string.
    /// </summary>
    /// <param name="guid">The FGuid string.</param>
    public FGuid(ReadOnlySpan<char> guid)
    {
        if (guid.Length != 32)
            throw new ArgumentOutOfRangeException(nameof(guid), "guid has to be 32 characters long, other parsing is not implemented");
        A = uint32.Parse(guid[..8], NumberStyles.AllowHexSpecifier);
        B = uint32.Parse(guid[8..16], NumberStyles.AllowHexSpecifier);
        C = uint32.Parse(guid[16..24], NumberStyles.AllowHexSpecifier);
        D = uint32.Parse(guid[24..32], NumberStyles.AllowHexSpecifier);
    }

    /// <summary>
    /// Parses a FGuid from a UTF8 string.
    /// </summary>
    /// <param name="utf8Guid">The UTF8 FGuid string.</param>
    public FGuid(ReadOnlySpan<byte> utf8Guid)
    {
        if (utf8Guid.Length != 32)
            throw new ArgumentOutOfRangeException(nameof(utf8Guid), "guid has to be 32 characters long, other parsing is not implemented");
        A = uint32.Parse(utf8Guid[..8], NumberStyles.AllowHexSpecifier);
        B = uint32.Parse(utf8Guid[8..16], NumberStyles.AllowHexSpecifier);
        C = uint32.Parse(utf8Guid[16..24], NumberStyles.AllowHexSpecifier);
        D = uint32.Parse(utf8Guid[24..32], NumberStyles.AllowHexSpecifier);
    }

    /// <summary>
    /// Creates a random FGuid.
    /// </summary>
    public static FGuid Random()
    {
        Unsafe.SkipInit(out FGuid result);
        RandomNumberGenerator.Fill(result.GetSpan());
        return result;
    }

    /// <summary>
    /// Checks for validity.
    /// </summary>
    public bool IsValid() => (A | B | C | D) != 0;

    /// <summary>
    /// Gets the data of the guid.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<uint32, byte>(ref Unsafe.AsRef(in A)), Size);

    /// <summary>
    /// Gets the values of the guid.
    /// </summary>
    public ReadOnlySpan<uint32> AsIntSpan() =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in A), 4);

    internal Span<byte> GetSpan() =>
        MemoryMarshal.CreateSpan(ref Unsafe.As<uint32, byte>(ref Unsafe.AsRef(in A)), Size);

    /// <inheritdoc />
    public bool Equals(FGuid other)
    {
        return A == other.A && B == other.B && C == other.C && D == other.D;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is FGuid other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(A, B, C, D);
    }

    /// <inheritdoc cref="Equals(FGuid)"/>
    public static bool operator ==(FGuid left, FGuid right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc cref="Equals(FGuid)"/>
    public static bool operator !=(FGuid left, FGuid right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc />
    public override string ToString() => GetHexString();

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        FormattableString formattable = $"{A:X8}{B:X8}{C:X8}{D:X8}";
        return formattable.ToString(formatProvider);
    }

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return destination.TryWrite(provider, $"{A:X8}{B:X8}{C:X8}{D:X8}", out charsWritten);
    }

    /// <inheritdoc />
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return Utf8.TryWrite(destination, provider, $"{A:X8}{B:X8}{C:X8}{D:X8}", out bytesWritten);
    }
}

/// <summary>
/// Converts <see cref="FGuid"/> from and to JSON.
/// </summary>
public sealed class FGuidConverter : JsonConverter<FGuid>
{
    /// <inheritdoc/>
    public override FGuid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.ValueSpan.IsEmpty) return default;
        return new FGuid(reader.ValueSpan);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, FGuid value, JsonSerializerOptions options)
    {
        Span<byte> guidUtf8 = stackalloc byte[FGuid.Size * 2];

        if (value.TryFormat(guidUtf8, out _, default, null))
        {
            writer.WriteStringValue(guidUtf8);
            return;
        }

        writer.WriteStringValue(string.Empty);
    }
}
