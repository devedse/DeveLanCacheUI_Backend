using OffiUtils;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;
// ReSharper disable InconsistentNaming
// ReSharper disable UseSymbolAlias

/// <summary>
/// UE FSHAHash struct
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = Size)]
public readonly struct FSHAHash : IEquatable<FSHAHash>, ISpanFormattable
{
    /// <summary>
    /// The size of the hash/struct.
    /// </summary>
    public const int Size = 20;

    private readonly long Hash_00_07;
    private readonly long Hash_08_15;
    private readonly int Hash_16_19;

    /// <inheritdoc/>
    public bool Equals(FSHAHash other)
    {
        return Hash_00_07 == other.Hash_00_07 && Hash_08_15 == other.Hash_08_15 && Hash_16_19 == other.Hash_16_19;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is FSHAHash other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Hash_00_07, Hash_08_15, Hash_16_19);
    }

    /// <inheritdoc cref="Equals(FSHAHash)"/>
    public static bool operator ==(FSHAHash left, FSHAHash right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc cref="Equals(FSHAHash)"/>
    public static bool operator !=(FSHAHash left, FSHAHash right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Gets the data of the hash.
    /// </summary>
    /// <returns>Hash data in a read-only span</returns>
    public ReadOnlySpan<byte> AsSpan() =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<long, byte>(ref Unsafe.AsRef(in Hash_00_07)), Size);

    internal Span<byte> GetSpan() =>
        MemoryMarshal.CreateSpan(ref Unsafe.As<long, byte>(ref Unsafe.AsRef(in Hash_00_07)), Size);

    /// <summary>
    /// Computes the hash of data using the SHA1 algorithm.
    /// </summary>
    /// <param name="source">The data to hash</param>
    /// <returns>The computed hash</returns>
    public static FSHAHash Compute(ReadOnlySpan<byte> source)
    {
        Unsafe.SkipInit(out FSHAHash hash);
        SHA1.TryHashData(source, hash.GetSpan(), out _);
        return hash;
    }

    /// <summary>Returns a <see cref="string"/> representation of the current <see cref="FSHAHash"/> instance.</summary>
    /// <returns>The value of this <see cref="FSHAHash"/>, represented as a series of uppercase hexadecimal digits.</returns>
    public override string ToString() => StringUtils.BytesToHexUpper(AsSpan());

    /// <summary>Returns a <see cref="string"/> representation of the current <see cref="FSHAHash"/> instance.</summary>
    /// <param name="upperCase">Whether or not to return an uppercase string.</param>
    /// <returns>The value of this <see cref="FSHAHash"/>, represented as a series of hexadecimal digits.</returns>
    public string ToString(bool upperCase) => upperCase
        ? StringUtils.BytesToHexUpper(AsSpan())
        : StringUtils.BytesToHexLower(AsSpan());

    /// <summary>Returns a <see cref="string"/> representation of the current <see cref="FSHAHash"/> instance, according to the provided format specifier.</summary>
    /// <param name="format">A read-only span containing the character representing one of the following specifiers that indicates the exact format to use when interpreting input:<br/>
    /// "x" or "X".<br/>
    /// When <paramref name="format"/> is <see langword="null"/> or empty, "X" is used.
    /// </param>
    /// <param name="formatProvider">Unused, pass a null reference.</param>
    /// <returns>The value of this <see cref="FSHAHash"/>, represented as a series of hexadecimal digits in the specified format.</returns>
    /// <exception cref="FormatException">If an invalid format is used.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (format is null || format.Length == 0 || format == "X")
            return StringUtils.BytesToHexUpper(AsSpan());
        if (format == "x")
            return StringUtils.BytesToHexLower(AsSpan());
        throw new FormatException("the provided format is not valid");
    }

    /// <summary>
    /// Tries to format the current <see cref="FSHAHash"/> instance into the provided character span.
    /// </summary>
    /// <param name="destination">The span in which to write the <see cref="FSHAHash"/> as a span of characters.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written into the span.</param>
    /// <param name="format">A read-only span containing the character representing one of the following specifiers that indicates the exact format to use when interpreting input:<br/>
    /// "x" or "X".<br/>
    /// When <paramref name="format"/> is empty, "X" is used.
    /// </param>
    /// <param name="provider">Unused, pass a null reference.</param>
    /// <returns><see langword="true"></see> if the formatting was successful; otherwise, <see langword="false"></see>.</returns>
    /// <exception cref="FormatException">If an invalid format is used.</exception>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (format.IsEmpty || format is "X")
            return StringUtils.TryWriteBytesToHexUpper(AsSpan(), destination, out charsWritten);
        if (format is "x")
            return StringUtils.TryWriteBytesToHexLower(AsSpan(), destination, out charsWritten);
        throw new FormatException("the provided format is not valid");
    }
}

/// <summary>
/// Converts <see cref="FSHAHash"/> from and to JSON.
/// </summary>
public sealed class FSHAHashConverter : JsonConverter<FSHAHash>
{
    /// <inheritdoc/>
    public override FSHAHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Unsafe.SkipInit(out FSHAHash result);
        var resultSpan = result.GetSpan();
        var span = reader.ValueSpan;

        for (var i = 0; i < resultSpan.Length; i++)
        {
            resultSpan[i] = byte.Parse(span.Slice(i * 2, 2), NumberStyles.AllowHexSpecifier);
        }

        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, FSHAHash value, JsonSerializerOptions options)
    {
        Span<char> hashUtf16 = stackalloc char[FSHAHash.Size * 2];

        if (value.TryFormat(hashUtf16, out _, default, null))
        {
            writer.WriteStringValue(hashUtf16);
            return;
        }

        writer.WriteStringValue(string.Empty);
    }
}
