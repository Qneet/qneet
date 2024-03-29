using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    private uint m_currentOffset = 0;

    public void Seek(int offset) => Seek((uint)offset);

    public void Seek(uint offset)
    {
        CheckBounds(0, offset);
        SeekNoCheck(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SeekBegin()
    {
        m_currentOffset = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SeekNoCheck(uint offset)
    {
        m_currentOffset = offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SkipNoCheck(int count) => SkipNoCheck((uint)count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SkipNoCheck(uint count)
    {
        m_currentOffset += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SkipNoCheck<T>() where T : unmanaged
    {
        m_currentOffset += (uint)sizeof(T);
    }

    public short ReadInt16NoCheck() => (short)ReadUInt16NoCheck();

    public ushort ReadUInt16()
    {
        CheckBounds(sizeof(short));
        return ReadUInt16NoCheck();
    }

    public ushort ReadUInt16NoCheck()
    {
        var value = ReadUInt16LittleEndian(m_pointer + m_currentOffset);
        m_currentOffset += sizeof(short);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ReadUInt16LittleEndian(byte* source)
    {
        return BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(source)
            : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(source));
    }

    public int ReadInt32NoCheck() => (int)ReadUInt32NoCheck();

    public uint ReadUInt32()
    {
        CheckBounds(sizeof(uint));
        return ReadUInt32NoCheck();
    }

    public uint ReadUInt32NoCheck()
    {
        var value = ReadUInt32LittleEndian(m_pointer + m_currentOffset);
        m_currentOffset += sizeof(uint);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32LittleEndian(byte* source)
    {
        return BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<uint>(source)
            : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(source));
    }

    /// <summary>
    /// Reads a fixed-length byte block as a null-padded UTF-8 encoded string.
    /// The padding is not included in the returned string.
    ///
    /// Note that it is legal for UTF-8 strings to contain NUL; if NUL occurs
    /// between non-NUL codepoints, it is not considered to be padding and
    /// is included in the result.
    /// </summary>
    public MemoryBlock ReadNullPaddedUTF8NoCheck(uint byteCount)
    {
        var bytes = m_pointer + m_currentOffset;
        m_currentOffset += byteCount;
        uint nonPaddedLength = 0;
        for (var i = byteCount; i > 0; --i)
        {
            if (bytes[i - 1] != 0)
            {
                nonPaddedLength = i;
                break;
            }
        }
        return new MemoryBlock(bytes, nonPaddedLength);
    }

    internal readonly void CheckBounds(uint count)
    {
        // Add cannot overflow because the worst case is (ulong)long.MaxValue + uint.MaxValue < ulong.MaxValue.
        if ((ulong)m_currentOffset + count > (ulong)m_maxOffset)
        {
            ThrowImageTooSmall();
        }
    }

    internal readonly void CheckBounds(long startPosition, uint count)
    {
        // Add cannot overflow because the worst case is (ulong)long.MaxValue + uint.MaxValue < ulong.MaxValue.
        // Negative count is handled by overflow to greater than maximum size = int.MaxValue.
        if ((ulong)startPosition + unchecked(count) > (ulong)m_maxOffset)
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
