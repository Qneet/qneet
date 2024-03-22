using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

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

    internal static unsafe ref byte AsTailRef(this BlobReader blobReader, int endOffset)
    {
        return ref Unsafe.AsRef<byte>(blobReader.StartPointer + blobReader.Length - endOffset);
    }
}
