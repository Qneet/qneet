using System.Diagnostics;

namespace Qnit.TestAdapter;

public readonly record struct ValueStopwatch
{
    private static readonly double s_timestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    private readonly long m_startTimestamp;

    public bool IsActive => m_startTimestamp != 0;

    private ValueStopwatch(long startTimestamp)
    {
        m_startTimestamp = startTimestamp;
    }

    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

    public TimeSpan GetElapsedTime()
    {
        // Start timestamp can't be zero in an initialized ValueStopwatch. It would have to be literally the first thing executed when the machine boots to be 0.
        // So it being 0 is a clear indication of default(ValueStopwatch)
        if (!IsActive)
        {
            ThrowNotInitialize();
        }

        var end = Stopwatch.GetTimestamp();
        var timestampDelta = end - m_startTimestamp;
        var ticks = (long)(s_timestampToTicks * timestampDelta);
        return new TimeSpan(ticks);
    }

    [DoesNotReturn]
    private static void ThrowNotInitialize()
    {
        throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");
    }
}
