#if NET9_0_OR_GREATER
global using LockObject = System.Threading.Lock;
global using ManifestData = System.Span<byte>;
global using ManifestReader = GenericReader.GenericSpanReader;
global using ManifestRoData = System.ReadOnlySpan<byte>;
#else
global using ManifestData = System.Memory<byte>;
global using ManifestRoData = System.ReadOnlyMemory<byte>;
global using ManifestReader = GenericReader.GenericBufferReader;
global using LockObject = System.Object;
#endif
