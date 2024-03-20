namespace Qneet.TestAdapter;

internal static class ExecutorUri
{
    internal const string String = "executor://qneet/v1";
    internal static readonly Uri Uri = new(String);
    internal static ReadOnlySpan<byte> Utf8Bytes => "executor://qneet/v1"u8;
}
