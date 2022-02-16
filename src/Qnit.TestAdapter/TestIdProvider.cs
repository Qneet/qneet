using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qnit.TestAdapter;

[SkipLocalsInit]
[SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "<Pending>")]
internal struct TestIdProvider
{
    internal const int BlockBits = 512;
    internal const int DigestBits = 160;
    public const int BlockBytes = BlockBits / 8;
    public const int DigestBytes = DigestBits / 8;

    private byte[] m_lastBlock = new byte[BlockBytes];

    private int m_position = 0;

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
        var end = Math.Min(BlockBytes - m_position, bytes.Length);
        bytes.Slice(0, end).CopyTo(m_lastBlock.AsSpan(m_position));

        // Block length is not reached yet.
        if (end + m_position < BlockBytes)
        {
            m_position += end;
            return;
        }

        m_hasher.ProcessBlock(m_lastBlock);
        m_position = 0;

        // We processed the entire string already
        if (end == bytes.Length)
        {
            return;
        }

        var start = 0;
        while (end < bytes.Length)
        {
            start = end;
            end += BlockBytes;
            if (end > bytes.Length)
            {
                break;
            }

            m_hasher.ProcessBlock(bytes.Slice(start, end - start));
        }

        if (end > bytes.Length)
        {
            m_position = bytes.Length - start;
            bytes.Slice(start, m_position).CopyTo(m_lastBlock);
        }
    }

    public void GetHash(Span<byte> hash)
    {
        if (m_position != 0)
        {
            m_hasher.PadMessage(ref m_lastBlock, m_position);
            m_hasher.ProcessBlock(m_lastBlock);
        }

        m_hasher.ProcessFinalBlock(hash);
    }

    public Guid GetId()
    {

        Span<byte> toGuid = stackalloc byte[DigestBytes];
        GetHash(toGuid);
        return new Guid(toGuid.Slice(0, 16));
    }

    /// <summary>
    /// SHA-1 Implementation as in https://tools.ietf.org/html/rfc3174
    /// </summary>
    /// <remarks>
    /// This implementation only works with messages with a length
    /// that is a multiple of the size of 8-bits.
    /// </remarks>
   
    public struct Sha1Implementation
    {
        /*
         * Many of the variable, function and parameter names in this code
         * were used because those were the names used in the publication.
         *
         * For more information please refer to https://tools.ietf.org/html/rfc3174.
         */

        private int m_streamSize = 0;
        private bool m_messagePadded = false;
        private readonly uint[] m_h = GC.AllocateUninitializedArray<uint>(5);

        public Sha1Implementation()
        {
            Reset();
        }

        /// <summary>
        /// A sequence of logical functions to be used in SHA-1.
        /// Each f(t), 0 <= t <= 79, operates on three 32-bit words B, C, D and produces a 32-bit word as output.
        /// </summary>
        /// <param name="t">Function index. 0 <= t <= 79</param>
        /// <param name="b">Word B</param>
        /// <param name="c">Word C</param>
        /// <param name="d">Word D</param>
        /// <returns>
        /// f(t;B,C,D) = (B AND C) OR ((NOT B) AND D)         ( 0 <= t <= 19)
        /// f(t;B,C,D) = B XOR C XOR D                        (20 <= t <= 39)
        /// f(t;B,C,D) = (B AND C) OR (B AND D) OR (C AND D)  (40 <= t <= 59)
        /// f(t;B,C,D) = B XOR C XOR D                        (60 <= t <= 79)
        /// </returns>
        private static uint F(int t, uint b, uint c, uint d)
        {
            switch (t)
            {
                case >= 0 and <= 19:
                    return (b & c) | (~b & d);
                case >= 20 and <= 39:
                case >= 60 and <= 79:
                    return b ^ c ^ d;
                case >= 40 and <= 59:
                    return (b & c) | (b & d) | (c & d);
                default:
                    ThrowArgumentOutOfBounds(nameof(t));
                    // This code never reached
                    return 0;
            }
        }

        /// <summary>
        /// Returns a constant word K(t) which is used in the SHA-1.
        /// </summary>
        /// <param name="t">Word index.</param>
        /// <returns>
        /// K(t) = 0x5A827999 ( 0 <= t <= 19)
        /// K(t) = 0x6ED9EBA1 (20 <= t <= 39)
        /// K(t) = 0x8F1BBCDC (40 <= t <= 59)
        /// K(t) = 0xCA62C1D6 (60 <= t <= 79)
        /// </returns>
        private static uint K(int t)
        {
            switch (t)
            {
                case >= 0 and <= 19:
                    return 0x5A827999u;
                case >= 20 and <= 39:
                    return 0x6ED9EBA1u;
                case >= 40 and <= 59:
                    return 0x8F1BBCDCu;
                case >= 60 and <= 79:
                    return 0xCA62C1D6u;
                default:
                    ThrowArgumentOutOfBounds(nameof(t));
                    // This code never reached
                    return 0;
            }
        }

        /// <summary>
        /// The circular left shift operation.
        /// </summary>
        /// <param name="x">An uint word.</param>
        /// <param name="n">0 <= n < 32</param>
        /// <returns>S^n(X)  =  (X << n) OR (X >> 32-n)</returns>
        private static uint S(uint x, byte n)
        {
            if (n <= 32)
                return (x << n) | (x >> (32 - n));

            ThrowArgumentOutOfBounds(nameof(n));
            // This code never reached
            return 0;
        }

        [DoesNotReturn]
        private static void ThrowArgumentOutOfBounds(string name)
        {
            throw new ArgumentException("Argument out of bounds! 0 <= t < 80", name);
        }

        private void Reset()
        {
            m_streamSize = 0;
            m_messagePadded = false;

            // as defined in https://tools.ietf.org/html/rfc3174#section-6.1
            m_h[0] = 0x67452301u;
            m_h[1] = 0xEFCDAB89u;
            m_h[2] = 0x98BADCFEu;
            m_h[3] = 0x10325476u;
            m_h[4] = 0xC3D2E1F0u;
        }

        public void ComputeHash(byte[] message, Span<byte> hash)
        {
            Reset();
            m_streamSize = 0;
            PadMessage(ref message);

            ProcessBlock(message);

            ProcessFinalBlock(hash);
        }

        private void ProcessMultipleBlocks(ReadOnlySpan<byte> message)
        {
            var messageCount = message.Length / BlockBytes;
            for (var i = 0; i < messageCount; i += 1)
            {
                ProcessBlock(message.Slice(i * BlockBytes, BlockBytes));
            }
        }

        public void ProcessBlock(ReadOnlySpan<byte> message)
        {
            if (message.Length % BlockBytes != 0)
            {
                ThrowInvalidBlockSize(message.Length);
            }

            if (message.Length != BlockBytes)
            {
                ProcessMultipleBlocks(message);
                return;
            }

            m_streamSize += BlockBytes;
            Span<uint> w = stackalloc uint[80];

            // Get W(0) .. W(15)
            for (var t = 0; t <= 15; t += 4)
            {
                w[t] = BinaryPrimitives.ReadUInt32BigEndian(message.Slice(t * sizeof(uint), sizeof(uint)));
                w[t + 1] = BinaryPrimitives.ReadUInt32BigEndian(message.Slice((t + 1) * sizeof(uint), sizeof(uint)));
                w[t + 2] = BinaryPrimitives.ReadUInt32BigEndian(message.Slice((t + 2) * sizeof(uint), sizeof(uint)));
                w[t + 3] = BinaryPrimitives.ReadUInt32BigEndian(message.Slice((t + 3) * sizeof(uint), sizeof(uint)));
            }

            // Calculate W(16) .. W(79)
            for (var t = 16; t <= 79; t++)
            {
                w[t] = S(w[t - 3] ^ w[t - 8] ^ w[t - 14] ^ w[t - 16], 1);
            }

            uint a = m_h[0],
                b = m_h[1],
                c = m_h[2],
                d = m_h[3],
                e = m_h[4];

            for (var t = 0; t < 80; t++)
            {
                var temp = S(a, 5) + F(t, b, c, d) + e + w[t] + K(t);
                e = d;
                d = c;
                c = S(b, 30);
                b = a;
                a = temp;
            }

            m_h[4] += e;
            m_h[3] += d;
            m_h[2] += c;
            m_h[1] += b;
            m_h[0] += a;

            [DoesNotReturn]
            static void ThrowInvalidBlockSize(int length)
            {
                throw new ArgumentException(
                    $"Invalid block size. Actual: {length}, Expected: Multiples of {BlockBytes}", nameof(message));
            }
        }

        public void ProcessFinalBlock(Span<byte> digest)
        {
            if (!m_messagePadded)
            {
                var pad = Array.Empty<byte>();
                PadMessage(ref pad, 0);
                ProcessBlock(pad);
            }

            BinaryPrimitives.WriteUInt32BigEndian(digest, m_h[0]);
            BinaryPrimitives.WriteUInt32BigEndian(digest.Slice(1 * sizeof(uint)), m_h[1]);
            BinaryPrimitives.WriteUInt32BigEndian(digest.Slice(2 * sizeof(uint)), m_h[2]);
            BinaryPrimitives.WriteUInt32BigEndian(digest.Slice(3 * sizeof(uint)), m_h[3]);
            BinaryPrimitives.WriteUInt32BigEndian(digest.Slice(4 * sizeof(uint)), m_h[4]);
        }

        public void PadMessage(ref byte[] message, int length = 0)
        {
            if (m_messagePadded)
            {
                ThrowInvalidOperationException();
            }

            if (length == 0)
            {
                length = message.Length;
            }
            else
            {
                Array.Resize(ref message, length);
            }

            m_streamSize += length;

            var paddingBytesCount = BlockBytes - (length % BlockBytes);

            // 64bit uint message size will be appended to end of the padding, making sure we have space for it.
            if (paddingBytesCount <= 8)
                paddingBytesCount += BlockBytes;

            Span<byte> padding = stackalloc byte[paddingBytesCount];
            padding.Clear();
            padding[0] = 0b10000000;

            var messageBits = (ulong)m_streamSize << 3;
            BinaryPrimitives.WriteUInt64BigEndian(padding.Slice(paddingBytesCount - sizeof(ulong)), messageBits);

            Array.Resize(ref message, message.Length + paddingBytesCount);
            padding.CopyTo(message.AsSpan(length));

            m_messagePadded = true;

            [DoesNotReturn]
            static void ThrowInvalidOperationException()
            {
                throw new InvalidOperationException();
            }
        }
    }
}
