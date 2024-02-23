namespace Qnit.TestAdapter;

internal static class ExecutorUri
{
    internal const string String = "executor://Qunit/v1";
    internal static readonly Uri Uri = new(String);
    internal static ReadOnlySpan<byte> Utf8Bytes => "executor://Qunit/v1"u8;
}
