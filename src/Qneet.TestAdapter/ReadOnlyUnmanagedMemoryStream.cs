namespace Qneet.TestAdapter;

internal sealed unsafe class ReadOnlyUnmanagedMemoryStream(byte* data, int length) : Stream
{
    private readonly byte* m_data = data;
    private readonly int m_length = length;
    private int m_position;

    public override unsafe int ReadByte()
    {
        if (m_position >= m_length)
        {
            return -1;
        }

        return m_data[m_position++];
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = Math.Min(count, m_length - m_position);
        new Span<byte>(m_data + m_position, bytesRead).CopyTo(buffer);
        m_position += bytesRead;
        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        var bytesRead = Math.Min(buffer.Length, m_length - m_position);
        new Span<byte>(m_data + m_position, bytesRead).CopyTo(buffer);
        m_position += bytesRead;
        return bytesRead;
    }

    public override void Flush()
    {
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => m_length;

    public override long Position
    {
        get
        {
            return m_position;
        }

        set
        {
            _ = Seek(value, SeekOrigin.Begin);
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long target;
        try
        {
            target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => checked(offset + m_position),
                SeekOrigin.End => checked(offset + m_length),
                _ => throw new ArgumentOutOfRangeException(nameof(origin)),
            };
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (target is < 0 or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        m_position = (int)target;
        return target;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
