using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Qnit.TestAdapter;

internal interface ITestCaseCollector
{
    public void AddTestCase(TestCase testCase);
}