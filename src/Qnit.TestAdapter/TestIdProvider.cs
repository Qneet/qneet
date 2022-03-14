using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qnit.TestAdapter;

[SkipLocalsInit]
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
        Span<byte> toGuid = stackalloc byte[Sha1Implementation.DigestBytes];

        m_hasher.ProcessFinalBlock(toGuid);
        return new Guid(toGuid.Slice(0, 16));
    }

    private void Reset()
    {
        m_hasher.Reset();
    }
}
