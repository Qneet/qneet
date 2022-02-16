using Xunit;

namespace Qnit.TestAdapter.Tests;

public static class ValueStopwatchTests
{
    public static void GetElapsedTimeThrowExceptionWhenDefaultConstructorUsed()
    {
        var sp = new ValueStopwatch();
        Assert.False(sp.IsActive);
        _ = Assert.Throws<InvalidOperationException>(() => sp.GetElapsedTime());
    }

    public static void GetElapsedTimeReturnElapsedWhenStartNewUsed()
    {
        var sp = ValueStopwatch.StartNew();

        var elapsed = sp.GetElapsedTime();

        Assert.True(sp.IsActive);
        Assert.NotEqual(TimeSpan.Zero, elapsed);
    }

    public static void SameObjectShouldBeEquals()
    {
        var sp = ValueStopwatch.StartNew();

        Assert.Equal(sp, sp);
    }

    public static void SameBoxedObjectShouldBeEquals()
    {
        var sp = ValueStopwatch.StartNew();

        Assert.Equal(sp, (object)sp);
    }

    public static void DifferentObjectShouldNotBeEquals()
    {
        var sp1 = ValueStopwatch.StartNew();
        new SpinWait().SpinOnce();
        var sp2 = ValueStopwatch.StartNew();

        Assert.NotEqual(sp1, sp2);
    }

    public static void UninitializedStopwatchHasZeroHashCode()
    {
        var sp = new ValueStopwatch();
        var hashCode = sp.GetHashCode();

        Assert.Equal(0, hashCode);
    }
}
