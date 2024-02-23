using System.Reflection.Metadata;

namespace Qnit.TestAdapter;

internal static class MetadataReaderExtensions
{
    internal static Span<byte> GetSpan(this MetadataReader metadataReader, StringHandle handle)
    {
        return metadataReader.GetBlobReader(handle).AsSpan();
    }

    internal static ReadOnlySpan<byte> GetReadOnlySpanSpan(this MetadataReader metadataReader, StringHandle handle)
    {
        return handle.IsNil ? [] : metadataReader.GetBlobReader(handle).AsReadonlySpan();
    }
}
