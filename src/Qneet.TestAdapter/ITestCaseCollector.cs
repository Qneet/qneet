using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Qneet.TestAdapter;

internal interface ITestCaseCollector
{
    public void AddTestCase(TestCase testCase);
}