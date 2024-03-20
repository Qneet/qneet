using System.Reflection.Metadata;

namespace Qneet.TestAdapter;

internal static class BlobReaderExtensions
{
    internal static unsafe Span<byte> AsSpan(this BlobReader blobReader)
    {
        return new Span<byte>(blobReader.StartPointer, blobReader.Length);
    }

    internal static unsafe ReadOnlySpan<byte> AsReadonlySpan(this BlobReader blobReader)
    {
        return new ReadOnlySpan<byte>(blobReader.StartPointer, blobReader.Length);
    }

    internal static unsafe ReadOnlySpan<byte> AsReadonlySpan(this BlobReader blobReader, int start)
    {
        return new ReadOnlySpan<byte>(blobReader.StartPointer, blobReader.Length).Slice(start);
    }
}
