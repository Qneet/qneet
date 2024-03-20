namespace Qneet.TestAdapter;

internal static class TestSource
{
    internal static bool Supported(string source)
    {
        var ext = Path.GetExtension(source.AsSpan());
        return ext is VsTestDiscoverer.DllExtension || ext is VsTestDiscoverer.ExeExtension;
    }
}
