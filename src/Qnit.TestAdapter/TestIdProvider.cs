using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

/// <summary>
/// SHA-1 Implementation as in https://tools.ietf.org/html/rfc3174
/// </summary>
/// <remarks>
/// This implementation only works with messages with a length
/// that is a multiple of the size of 8-bits.
/// </remarks>
[SkipLocalsInit]
public struct Sha1Implementation
{
    private const int BlockBits = 512;
    private const int DigestBits = 160;
    public const int BlockBytes = BlockBits / 8;
    public const int DigestBytes = DigestBits / 8;
    /*
     * Many of the variable, function and parameter names in this code
     * were used because those were the names used in the publication.
     *
     * For more information please refer to https://tools.ietf.org/html/rfc3174.
     */

    private uint[] m_h;
    private byte[] m_lastBlock;

    private int m_count0;
    private int m_count1;

    public Sha1Implementation()
    {
        m_lastBlock = new byte[BlockBytes];
        m_h = new uint[5];
        m_count0 = 0;
        m_count1 = 0;

        Reset();
    }

    internal void Reset()
    {
        m_count0 = 0;
        m_count1 = 0;

        // as defined in https://tools.ietf.org/html/rfc3174#section-6.1
        m_h[0] = 0x67452301u;
        m_h[1] = 0xEFCDAB89u;
        m_h[2] = 0x98BADCFEu;
        m_h[3] = 0x10325476u;
        m_h[4] = 0xC3D2E1F0u;
    }

    private void Transform(ReadOnlySpan<byte> message)
    {
        var e = m_h[4];
        var d = m_h[3];
        var c = m_h[2];
        var b = m_h[1];
        var a = m_h[0];

        Span<byte> buffer = stackalloc byte[BlockBytes];
        message.Slice(0, BlockBytes).CopyTo(buffer);

        ref var block = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, uint>(buffer));

        /* 4 rounds of 20 operations each. Loop unrolled. */
        R0(ref block, a, ref b, c, d, ref e, 0); R0(ref block, e, ref a, b, c, ref d, 1);
        R0(ref block, d, ref e, a, b, ref c, 2); R0(ref block, c, ref d, e, a, ref b, 3);
        R0(ref block, b, ref c, d, e, ref a, 4); R0(ref block, a, ref b, c, d, ref e, 5);
        R0(ref block, e, ref a, b, c, ref d, 6); R0(ref block, d, ref e, a, b, ref c, 7);
        R0(ref block, c, ref d, e, a, ref b, 8); R0(ref block, b, ref c, d, e, ref a, 9);
        R0(ref block, a, ref b, c, d, ref e, 10); R0(ref block, e, ref a, b, c, ref d, 11);
        R0(ref block, d, ref e, a, b, ref c, 12); R0(ref block, c, ref d, e, a, ref b, 13);
        R0(ref block, b, ref c, d, e, ref a, 14); R0(ref block, a, ref b, c, d, ref e, 15);

        R1(ref block, e, ref a, b, c, ref d, 0); R1(ref block, d, ref e, a, b, ref c, 1);
        R1(ref block, c, ref d, e, a, ref b, 2); R1(ref block, b, ref c, d, e, ref a, 3);

        R2(ref block, a, ref b, c, d, ref e, 4); R2(ref block, e, ref a, b, c, ref d, 5);
        R2(ref block, d, ref e, a, b, ref c, 6); R2(ref block, c, ref d, e, a, ref b, 7);
        R2(ref block, b, ref c, d, e, ref a, 8); R2(ref block, a, ref b, c, d, ref e, 9);
        R2(ref block, e, ref a, b, c, ref d, 10); R2(ref block, d, ref e, a, b, ref c, 11);
        R2(ref block, c, ref d, e, a, ref b, 12); R2(ref block, b, ref c, d, e, ref a, 13);
        R2(ref block, a, ref b, c, d, ref e, 14); R2(ref block, e, ref a, b, c, ref d, 15);
        R2(ref block, d, ref e, a, b, ref c, 0); R2(ref block, c, ref d, e, a, ref b, 1);
        R2(ref block, b, ref c, d, e, ref a, 2); R2(ref block, a, ref b, c, d, ref e, 3);
        R2(ref block, e, ref a, b, c, ref d, 4); R2(ref block, d, ref e, a, b, ref c, 5);
        R2(ref block, c, ref d, e, a, ref b, 6); R2(ref block, b, ref c, d, e, ref a, 7);

        R3(ref block, a, ref b, c, d, ref e, 8); R3(ref block, e, ref a, b, c, ref d, 9);
        R3(ref block, d, ref e, a, b, ref c, 10); R3(ref block, c, ref d, e, a, ref b, 11);
        R3(ref block, b, ref c, d, e, ref a, 12); R3(ref block, a, ref b, c, d, ref e, 13);
        R3(ref block, e, ref a, b, c, ref d, 14); R3(ref block, d, ref e, a, b, ref c, 15);
        R3(ref block, c, ref d, e, a, ref b, 0); R3(ref block, b, ref c, d, e, ref a, 1);
        R3(ref block, a, ref b, c, d, ref e, 2); R3(ref block, e, ref a, b, c, ref d, 3);
        R3(ref block, d, ref e, a, b, ref c, 4); R3(ref block, c, ref d, e, a, ref b, 5);
        R3(ref block, b, ref c, d, e, ref a, 6); R3(ref block, a, ref b, c, d, ref e, 7);
        R3(ref block, e, ref a, b, c, ref d, 8); R3(ref block, d, ref e, a, b, ref c, 9);
        R3(ref block, c, ref d, e, a, ref b, 10); R3(ref block, b, ref c, d, e, ref a, 11);

        R4(ref block, a, ref b, c, d, ref e, 12); R4(ref block, e, ref a, b, c, ref d, 13);
        R4(ref block, d, ref e, a, b, ref c, 14); R4(ref block, c, ref d, e, a, ref b, 15);
        R4(ref block, b, ref c, d, e, ref a, 0); R4(ref block, a, ref b, c, d, ref e, 1);
        R4(ref block, e, ref a, b, c, ref d, 2); R4(ref block, d, ref e, a, b, ref c, 3);
        R4(ref block, c, ref d, e, a, ref b, 4); R4(ref block, b, ref c, d, e, ref a, 5);
        R4(ref block, a, ref b, c, d, ref e, 6); R4(ref block, e, ref a, b, c, ref d, 7);
        R4(ref block, d, ref e, a, b, ref c, 8); R4(ref block, c, ref d, e, a, ref b, 9);
        R4(ref block, b, ref c, d, e, ref a, 10); R4(ref block, a, ref b, c, d, ref e, 11);
        R4(ref block, e, ref a, b, c, ref d, 12); R4(ref block, d, ref e, a, b, ref c, 13);
        R4(ref block, c, ref d, e, a, ref b, 14); R4(ref block, b, ref c, d, e, ref a, 15);

        m_h[4] += e;
        m_h[3] += d;
        m_h[2] += c;
        m_h[1] += b;
        m_h[0] += a;


    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint blk0(ref uint blockRef, int i)
    {
        ref var value = ref Unsafe.Add(ref blockRef, i);
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint blk(ref uint blockRef, int i)
    {
        ref var oldValue = ref Unsafe.Add(ref blockRef, i & 15);
        var value = BitOperations.RotateLeft(
            Unsafe.Add(ref blockRef, (i + 13) & 15)
            ^ Unsafe.Add(ref blockRef, (i + 8) & 15) 
            ^ Unsafe.Add(ref blockRef, (i + 2) & 15)
            ^ oldValue, 1);

        oldValue = value;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void R0(ref uint block, uint v, ref uint w, uint x, uint y, ref uint z, int i)
    {
        var value = blk0(ref block, i);
        z += ((w & (x ^ y)) ^ y) + value + 0x5a827999 + BitOperations.RotateLeft(v, 5);
        w = BitOperations.RotateLeft(w, 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void R1(ref uint block, uint v, ref uint w, uint x, uint y, ref uint z, int i)
    {
        var value = blk(ref block, i);
        z += ((w & (x ^ y)) ^ y) + value + 0x5a827999 + BitOperations.RotateLeft(v, 5);
        w = BitOperations.RotateLeft(w, 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void R2(ref uint block, uint v, ref uint w, uint x, uint y, ref uint z, int i)
    {
        var value = blk(ref block, i);
        z += (w ^ x ^ y) + value + 0x6ed9eba1 + BitOperations.RotateLeft(v, 5);
        w = BitOperations.RotateLeft(w, 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void R3(ref uint block, uint v, ref uint w, uint x, uint y, ref uint z, int i)
    {
        var value = blk(ref block, i);
        z += (((w | x) & y) | (w & x)) + value + 0x8f1bbcdc + BitOperations.RotateLeft(v, 5);
        w = BitOperations.RotateLeft(w, 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void R4(ref uint block, uint v, ref uint w, uint x, uint y, ref uint z, int i)
    {
        var value = blk(ref block, i);
        z += (w ^ x ^ y) + value + 0xca62c1d6 + BitOperations.RotateLeft(v, 5);
        w = BitOperations.RotateLeft(w, 30);
    }

    public void ProcessBlock(ReadOnlySpan<byte> message)
    {
        if (message.IsEmpty)
        {
            return;
        }

        /* Compute number of bytes mod 64 */
        var index = (m_count0 >> 3) & 63;

        /* Update number of bits */
        if ((m_count0 += (message.Length << 3)) < (message.Length << 3))
        {
            m_count1++;
        }

        m_count1 += (message.Length >> 29);

        var partLen = BlockBytes - index;

        /* Transform as many times as possible. */
        var i = 0;
        if (message.Length >= partLen)
        {
            if (index != 0)
            {
                message.Slice(0, partLen).CopyTo(m_lastBlock.AsSpan(index));
                Transform(m_lastBlock);
                i = partLen;
            }

            for (; i + 63 < message.Length; i += BlockBytes)
            {
                Transform(message.Slice(i));
            }

            if (message.Length == i)
                return;

            index = 0;
        }

        /* Buffer remaining input */
        message.Slice(i, message.Length - i).CopyTo(m_lastBlock.AsSpan(index));
    }

    public void ProcessFinalBlock(Span<byte> digest)
    {
        Span<byte> finalCount = stackalloc byte[8];
        for (var i = 0; i < 8; i++)
        {
            finalCount[i] = (byte)((i < 4 ? m_count1 : m_count0) >> ((3 - (i & 3)) * 8) & 255);
        }

        Span<byte> temp = stackalloc byte[1];
        temp[0] = 0b10000000;
        ProcessBlock(temp);
        while ((m_count0 & 504) != 448)
        {
            temp[0] = 0;
            ProcessBlock(temp);
        }
        ProcessBlock(finalCount);

        BinaryPrimitives.WriteUInt32BigEndian(digest, m_h[0]);
        BinaryPrimitives.WriteUInt32BigEndian(digest.Slice(1 * sizeof(uint)), m_h[1]);
        BinaryPrimitives.WriteUInt32BigEndian(digest.Slice(2 * sizeof(uint)), m_h[2]);
        BinaryPrimitives.WriteUInt32BigEndian(digest.Slice(3 * sizeof(uint)), m_h[3]);
        BinaryPrimitives.WriteUInt32BigEndian(digest.Slice(4 * sizeof(uint)), m_h[4]);
    }

    public void ComputeHash(ReadOnlySpan<byte> message, Span<byte> digest)
    {
        ProcessBlock(message);
        ProcessFinalBlock(digest);
    }
}
