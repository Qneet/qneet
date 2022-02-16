namespace Qnit.TestAdapter;

internal static class TestSource
{
    internal static bool Supported(string source)
    {
        var ext = Path.GetExtension(source.AsSpan());
        return ext.SequenceEqual(VsTestDiscoverer.DllExtension)
               || ext.SequenceEqual(VsTestDiscoverer.ExeExtension);
    }
}
