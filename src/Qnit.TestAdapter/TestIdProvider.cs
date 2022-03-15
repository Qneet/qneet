using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Qnit.TestAdapter;

[SkipLocalsInit]
[StructLayout(LayoutKind.Auto)]
internal struct TestIdProvider
{
    // should not be readonly
    private Sha1Implementation m_hasher;

    public TestIdProvider()
    {
        m_hasher = new Sha1Implementation();
    }

    public void Append(string str)
    {
        var byteCount = Encoding.Unicode.GetByteCount(str);

        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        var bytes = new Span<byte>(buffer, 0, byteCount);
        _ = Encoding.Unicode.GetBytes(str, bytes);

        Append(bytes);

        ArrayPool<byte>.Shared.Return(buffer);
    }

    public unsafe void Append(byte value)
    {
        var b = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref value), 1);
        Append(b);
    }

    public void Append(ReadOnlySpan<byte> bytes)
    {
        m_hasher.ProcessBlock(bytes);
    }

    public Guid GetIdAndReset()
    {
        var id = GetId();
        Reset();
        return id;
    }

    private Guid GetId()
    {
        Span<byte> digest = stackalloc byte[Sha1Implementation.DigestBytes];

        m_hasher.ProcessFinalBlock(digest);
        return new Guid(digest.Slice(0, 16));
    }

    private void Reset()
    {
        m_hasher.Reset();
    }
}
