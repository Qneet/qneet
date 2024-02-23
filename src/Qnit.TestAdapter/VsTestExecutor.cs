using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Qnit.TestAdapter;

[ExtensionUri(ExecutorUri.String)]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Only cancel can dispose m_cancellationSource field")]
public sealed class VsTestExecutor : ITestExecutor
{
    private readonly struct TestTestCaseCollector : ITestCaseCollector
    {
        private readonly List<TestCase> m_testCases = [];

        public TestTestCaseCollector()
        {
            m_testCases = [];
        }

        public void AddTestCase(TestCase testCase)
        {
            m_testCases.Add(testCase);
        }

        public List<TestCase> TestCases => m_testCases;
    }

    private CancellationTokenSource? m_cancellationSource;

    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (tests == null)
            return;
        ArgumentNullException.ThrowIfNull(frameworkHandle);

        m_cancellationSource = new CancellationTokenSource();
        var cancellationSource = m_cancellationSource;
        using var countdownEvent = new CountdownEvent(1);

        var groups = tests.GroupBy(s => s.Source, StringComparer.Ordinal);
        foreach (var g in groups)
        {
            ExecuteTest(g.Key, g, frameworkHandle, countdownEvent, cancellationSource.Token);
        }

        _ = countdownEvent.Signal();

        countdownEvent.Wait(cancellationSource.Token);
        cancellationSource.Cancel(throwOnFirstException: false);
        cancellationSource.Dispose();
        m_cancellationSource = null;
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (sources == null)
            return;
        ArgumentNullException.ThrowIfNull(frameworkHandle);

        m_cancellationSource = new CancellationTokenSource();
        var cancellationSource = m_cancellationSource;
        using var countdownEvent = new CountdownEvent(1);

        var testCaseCollector = new TestTestCaseCollector();
        var discover = new Discoverer(frameworkHandle);
        foreach (var source in sources)
        {
            if (cancellationSource.IsCancellationRequested)
            {
                break;
            }
            discover.Discover(source, testCaseCollector, cancellationSource.Token);

            ExecuteTest(source, testCaseCollector.TestCases, frameworkHandle, countdownEvent, cancellationSource.Token);

            testCaseCollector.TestCases.Clear();
        }

        countdownEvent.Signal();

        countdownEvent.Wait(cancellationSource.Token);
        cancellationSource.Cancel(throwOnFirstException: false);
        m_cancellationSource = null;
    }

    private static void ExecuteTest<TList>(string source, TList tests, IFrameworkHandle frameworkHandle, CountdownEvent countdownEvent, CancellationToken cancellationToken)
        where TList : IEnumerable<TestCase>
    {
#pragma warning disable S3885
        var asm = Assembly.LoadFile(source);
#pragma warning restore S3885
        foreach (var test in tests)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            countdownEvent.AddCount();
            var executor = new TestCaseExecutor(asm, test, frameworkHandle, countdownEvent, cancellationToken);
            //executor.Execute();
            ThreadPool.UnsafeQueueUserWorkItem(executor, preferLocal: false);
        }
    }

    public void Cancel()
    {
        m_cancellationSource?.Cancel();
    }
}

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "By design")]
internal sealed class TestCaseExecutor(Assembly assembly, TestCase testCase, IFrameworkHandle mFrameworkHandle,
    CountdownEvent countdownEvent, CancellationToken cancellationToken) : IThreadPoolWorkItem
{
    private readonly Assembly m_assembly = assembly;
    private readonly TestCase m_testCase = testCase;
    private readonly IFrameworkHandle m_frameworkHandle = mFrameworkHandle;
    private readonly CountdownEvent m_countdownEvent = countdownEvent;
    private readonly CancellationToken m_cancellationToken = cancellationToken;

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Can not simplified")]
    public void Execute()
    {
        try
        {
            if (m_cancellationToken.IsCancellationRequested)
            {
                // skip running test
                return;
            }

            var testResult = new TestResult(m_testCase)
            {
                DisplayName = m_testCase.DisplayName,
                // set by constructor
                //StartTime = DateTimeOffset.Now
            };

            try
            {
                m_frameworkHandle.RecordStart(m_testCase);

                var typeName = m_testCase.GetPropertyValue(ManagedNameConstants.ManagedTypeProperty, string.Empty);
                var type = m_assembly.GetType(typeName, throwOnError: true)!;

                var methodName = m_testCase.GetPropertyValue(ManagedNameConstants.ManagedMethodProperty, string.Empty);
                const BindingFlags MethodBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
                var method = type.GetMethod(methodName, MethodBindingFlags) ?? throw new TestCaseNotFoundException(methodName);
                var stopWatch = ValueStopwatch.StartNew();
                try
                {
                    if (method.IsStatic)
                    {
                        method.Invoke(null, []);
                    }
                    else
                    {
                        var instance = Activator.CreateInstance(type);
                        method.Invoke(instance, []);
                    }

                    testResult.Duration = stopWatch.GetElapsedTime();
                    testResult.Outcome = TestOutcome.Passed;
                }
                catch (TargetInvocationException e)
                {
                    testResult.Duration = stopWatch.GetElapsedTime();
                    testResult.Outcome = TestOutcome.Failed;
                    if (e.InnerException != null)
                    {
                        testResult.ErrorMessage = e.InnerException.Message;
                        testResult.ErrorStackTrace = e.InnerException.StackTrace;
                    }
                    else
                    {
                        testResult.ErrorMessage = e.Message;
                        testResult.ErrorStackTrace = e.StackTrace;
                    }

                }

#pragma warning disable CA1031
                catch (Exception e)
#pragma warning restore CA1031
                {
                    testResult.Duration = stopWatch.GetElapsedTime();
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = e.Message;
                    testResult.ErrorStackTrace = e.StackTrace;
                }


                //testResult.SetPropertyValue<Guid>(ExecutorUri.ExecutionIdProperty, this.ExecutionId);
                //testResult.SetPropertyValue<Guid>(ExecutorUri.ParentExecIdProperty, this.ParentExecId);
                //testResult.SetPropertyValue<int>(ExecutorUri.InnerResultsCountProperty, this.InnerResultsCount);
            }
            finally
            {

                m_frameworkHandle.RecordEnd(m_testCase, testResult.Outcome);

                testResult.EndTime = DateTimeOffset.Now;
                try
                {
                    m_frameworkHandle.RecordResult(testResult);
                }
                catch (TestCanceledException)
                {
                    // Ignore this exception
                }
            }
        }
        finally
        {
            m_countdownEvent.Signal();
        }
    }
}
