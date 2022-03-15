using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Qnit.TestAdapter;

[SkipLocalsInit]
[StructLayout(LayoutKind.Auto)]
internal struct TestIdProvider
{
    [StructLayout(LayoutKind.Sequential, Size = Sha1Implementation.DigestBytes)]
    private struct Buffer20 { }

    // should not be readonly
    private Sha1Implementation m_hasher;
    private Buffer20 m_digestBuffer;

    public TestIdProvider()
    {
        m_hasher = new Sha1Implementation();
        Unsafe.SkipInit(out m_digestBuffer);
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
        var digest = MemoryMarshal.CreateSpan(ref Unsafe.As<Buffer20, byte>(ref m_digestBuffer), Sha1Implementation.DigestBytes);
        m_hasher.ProcessFinalBlock(digest);
        return new Guid(digest.Slice(0, 16));
    }

    private void Reset()
    {
        m_hasher.Reset();
    }
}
