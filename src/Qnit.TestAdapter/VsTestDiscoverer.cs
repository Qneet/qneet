using System.ComponentModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Qnit.TestAdapter;

[DefaultExecutorUri(ExecutorUri.String)]
[FileExtension(DllExtension)]
[FileExtension(ExeExtension)]
[Category("managed")]
public sealed class VsTestDiscoverer : ITestDiscoverer
{
    private readonly struct TestTestCaseCollector : ITestCaseCollector
    {
        private readonly ITestCaseDiscoverySink m_testCaseDiscoverySink;

        public TestTestCaseCollector(ITestCaseDiscoverySink testCaseDiscoverySink)
        {
            m_testCaseDiscoverySink = testCaseDiscoverySink;
        }

        public void AddTestCase(TestCase testCase)
        {
            m_testCaseDiscoverySink.SendTestCase(testCase);
        }
    }

    internal const string DllExtension = ".dll";
    internal const string ExeExtension = ".exe";

    public void DiscoverTests(
        IEnumerable<string>? sources,
        IDiscoveryContext discoveryContext,
        IMessageLogger logger,
        ITestCaseDiscoverySink discoverySink)
    {
        if (sources == null)
            return;

        var d = new Discoverer(logger);

        var testCaseCollector = new TestTestCaseCollector(discoverySink);
        foreach (var source in sources)
        {
            d.Discover(source, testCaseCollector, CancellationToken.None);
        }
    }
}

