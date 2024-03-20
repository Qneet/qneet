using Microsoft.VisualStudio.TestPlatform;
using Qneet.TestAdapter;

[assembly: TestExtensionTypes(typeof(VsTestDiscoverer), typeof(VsTestExecutor))]
