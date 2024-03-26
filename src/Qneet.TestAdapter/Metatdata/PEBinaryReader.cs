using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Qneet.TestAdapter.Metatdata;

/// <summary>
/// Simple BinaryReader wrapper to:
///
///  1) throw BadImageFormat instead of EndOfStream or ArgumentOutOfRange.
///  2) limit reads to a subset of the base stream.
///
/// Only methods that are needed to read PE headers are implemented.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal unsafe struct PEBinaryReader(byte* pointer, int size)
{
    private readonly long m_maxOffset = size;
    private readonly byte* m_pointer = pointer;
    private int m_currentOffset = 0;

    public readonly int CurrentOffset => m_currentOffset;

    public void Seek(int offset)
    {
        CheckBounds(0, offset);
        m_currentOffset = offset;
    }

    public void Skip(int count)
    {
        CheckBounds(m_currentOffset, count);
        m_currentOffset += count;
    }

    public void Skip<T>() where T : unmanaged
    {
        CheckBounds(m_currentOffset, sizeof(T));
        m_currentOffset += sizeof(T);
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var currentOffset = m_currentOffset;
        CheckBounds(currentOffset, count);
        var value = new ReadOnlySpan<byte>(m_pointer + currentOffset, count);
        m_currentOffset += count;
        return value;
    }

    public byte ReadByte()
    {
        CheckBounds(sizeof(byte));
        return m_pointer[m_currentOffset++];
    }

    public short ReadInt16() => (short)ReadUInt16();

    public ushort ReadUInt16()
    {
        CheckBounds(sizeof(short));
        var value = ReadUInt16LittleEndian(m_pointer + m_currentOffset);
        m_currentOffset += sizeof(short);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ReadUInt16LittleEndian(byte* source)
    {
        return !BitConverter.IsLittleEndian ?
            BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(source)) :
            Unsafe.ReadUnaligned<ushort>(source);
    }

    public int ReadInt32() => (int)ReadUInt32();

    public uint ReadUInt32()
    {
        CheckBounds(sizeof(uint));
        var value = ReadUInt32LittleEndian(m_pointer + m_currentOffset);
        m_currentOffset += sizeof(uint);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32LittleEndian(byte* source)
    {
        return !BitConverter.IsLittleEndian ?
            BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(source)) :
            Unsafe.ReadUnaligned<uint>(source);
    }

    public ulong ReadUInt64()
    {
        CheckBounds(sizeof(ulong));
        var value = ReadUInt64LittleEndian(m_pointer + m_currentOffset);
        m_currentOffset += sizeof(ulong);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadUInt64LittleEndian(byte* source)
    {
        return !BitConverter.IsLittleEndian ?
            BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(source)) :
            Unsafe.ReadUnaligned<ulong>(source);
    }

    /// <summary>
    /// Reads a fixed-length byte block as a null-padded UTF-8 encoded string.
    /// The padding is not included in the returned string.
    ///
    /// Note that it is legal for UTF-8 strings to contain NUL; if NUL occurs
    /// between non-NUL codepoints, it is not considered to be padding and
    /// is included in the result.
    /// </summary>
    public string ReadNullPaddedUTF8(int byteCount)
    {
        CheckBounds(m_currentOffset, byteCount);
        var bytes = m_pointer + m_currentOffset;
        m_currentOffset += byteCount;
        var nonPaddedLength = 0;
        for (var i = byteCount; i > 0; --i)
        {
            if (bytes[i - 1] != 0)
            {
                nonPaddedLength = i;
                break;
            }
        }
        return Encoding.UTF8.GetString(bytes, nonPaddedLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReadNUInt32LittleEndian(byte* source)
    {
        return !BitConverter.IsLittleEndian ?
            BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<nuint>(source)) :
            Unsafe.ReadUnaligned<nuint>(source);
    }

    private readonly void CheckBounds(uint count)
    {
        // Add cannot overflow because the worst case is (ulong)long.MaxValue + uint.MaxValue < ulong.MaxValue.
        if ((ulong)m_currentOffset + count > (ulong)m_maxOffset)
        {
            ThrowImageTooSmall();
        }
    }

    private readonly void CheckBounds(long startPosition, int count)
    {
        // Add cannot overflow because the worst case is (ulong)long.MaxValue + uint.MaxValue < ulong.MaxValue.
        // Negative count is handled by overflow to greater than maximum size = int.MaxValue.
        if ((ulong)startPosition + unchecked((uint)count) > (ulong)m_maxOffset)
        {
            ThrowImageTooSmallOrContainsInvalidOffsetOrCount();
        }
    }

    [DoesNotReturn]
    [SuppressMessage("Design", "MA0012:Do not raise reserved exception type", Justification = "<Pending>")]
    internal static void ThrowImageTooSmall()
    {
        throw new BadImageFormatException("Image is too small.");
    }

    [DoesNotReturn]
    [SuppressMessage("Design", "MA0012:Do not raise reserved exception type", Justification = "<Pending>")]
    internal static void ThrowImageTooSmallOrContainsInvalidOffsetOrCount()
    {
        throw new BadImageFormatException("Image is either too small or contains an invalid byte offset or count.");
    }
}
