using System.Text;

namespace Qnit.TestAdapter;

internal static class ExecutorUri
{
    internal const string String = "executor://Qunit/v1";
    internal static readonly Uri Uri = new(String);
    internal static readonly ReadOnlyMemory<byte> Utf8Bytes = Encoding.UTF8.GetBytes(String).AsMemory();
}
