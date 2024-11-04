using System.Collections;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Qneet.TestAdapter;

[ExtensionUri(ExecutorUri.String)]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Only cancel can dispose m_cancellationSource field")]
public sealed class VsTestExecutor : ITestExecutor
{
    private readonly struct ListTestCaseCollector : ITestCaseCollector, IEnumerable<TestCase>
    {
        private readonly List<TestCase> m_testCases = [];

        public ListTestCaseCollector()
        {
            m_testCases = [];
        }

        public void AddTestCase(TestCase testCase)
        {
            m_testCases.Add(testCase);
        }

        public void Clear() => m_testCases.Clear();

        public List<TestCase>.Enumerator GetEnumerator() => m_testCases.GetEnumerator();

        IEnumerator<TestCase> IEnumerable<TestCase>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)m_testCases).GetEnumerator();
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

        var testCaseCollector = new ListTestCaseCollector();
        var discover = new Discoverer(frameworkHandle);
        foreach (var source in sources)
        {
            if (cancellationSource.IsCancellationRequested)
            {
                break;
            }
            discover.Discover(source, testCaseCollector, cancellationSource.Token);

            ExecuteTest(source, testCaseCollector, frameworkHandle, countdownEvent, cancellationSource.Token);

            testCaseCollector.Clear();
        }

        _ = countdownEvent.Signal();

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
        var module = asm.ManifestModule;
        foreach (var test in tests)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            countdownEvent.AddCount();
            var executor = new TestCaseExecutor(module, test, frameworkHandle, countdownEvent, cancellationToken);
            //executor.Execute();
            _ = ThreadPool.UnsafeQueueUserWorkItem(executor, preferLocal: false);
        }
    }

    public void Cancel()
    {
        m_cancellationSource?.Cancel();
    }
}

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "By design")]
internal sealed class TestCaseExecutor(Module module, TestCase testCase, IFrameworkHandle frameworkHandle,
    CountdownEvent countdownEvent, CancellationToken cancellationToken) : IThreadPoolWorkItem
{
    private const int MethodTokenNotFound = -1;

    private readonly Module m_module = module;
    private readonly TestCase m_testCase = testCase;
    private readonly IFrameworkHandle m_frameworkHandle = frameworkHandle;
    private readonly CountdownEvent m_countdownEvent = countdownEvent;
    private readonly CancellationToken m_cancellationToken = cancellationToken;

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Can not simplified")]
    public unsafe void Execute()
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

                var methodToken = m_testCase.GetPropertyValue(ManagedNameConstants.MethodTokenProperty, MethodTokenNotFound);
                if (methodToken == MethodTokenNotFound)
                {
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = $"Test case \"{m_testCase.DisplayName}\" does not have method token property";
                }
                else
                {
                    var methodHandle = m_module.ModuleHandle.ResolveMethodHandle(methodToken);

                    /*
                    var typeName = m_testCase.GetPropertyValue(ManagedNameConstants.ManagedTypeProperty, string.Empty);
                    var type = m_assembly.GetType(typeName, throwOnError: true)!;
                    var methodName = m_testCase.GetPropertyValue(ManagedNameConstants.ManagedMethodProperty, string.Empty);
                    const BindingFlags MethodBindingFlags = BindingFlags.Public | BindingFlags.Static;
                    var method = type.GetMethod(methodName, 0, MethodBindingFlags, binder: null, types: [], modifiers: null) ?? throw new TestCaseNotFoundException(methodName);
                    var methodHandle = method.MethodHandle;
                    */

                    // support only static methods, without input paramters and return void
                    var testMethod = (delegate* managed<void>)methodHandle.GetFunctionPointer();
                    var stopWatch = ValueStopwatch.StartNew();
                    try
                    {
                        testMethod();

                        testResult.Duration = stopWatch.GetElapsedTime();
                        testResult.Outcome = TestOutcome.Passed;
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
                }


                //testResult.SetPropertyValue<Guid>(ExecutorUri.ExecutionIdProperty, this.ExecutionId);
                //testResult.SetPropertyValue<Guid>(ExecutorUri.ParentExecIdProperty, this.ParentExecId);
                //testResult.SetPropertyValue<int>(ExecutorUri.InnerResultsCountProperty, this.InnerResultsCount);
            }
#pragma warning disable CA1031
            catch (Exception e)
#pragma warning restore CA1031
            {
                testResult.Outcome = TestOutcome.Failed;
                testResult.ErrorMessage = e.Message;
                testResult.ErrorStackTrace = e.StackTrace;
            }
            finally
            {

                m_frameworkHandle.RecordEnd(m_testCase, testResult.Outcome);

                testResult.EndTime = DateTimeOffset.UtcNow;
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
            _ = m_countdownEvent.Signal();
        }
    }
}
