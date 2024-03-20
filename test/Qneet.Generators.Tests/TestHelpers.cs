using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Qneet.Generators.Tests;
internal static class TestHelpers
{
    public static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput<T>(params string[] source)
        where T : IIncrementalGenerator, new()
    {
        var (diagnostics, trees) = GetGeneratedTrees<T, TrackingNames>(source);
        return (diagnostics, trees.LastOrDefault() ?? string.Empty);
    }

    public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedTrees<TGenerator, TTrackingNames>(params string[] sources)
        where TGenerator : IIncrementalGenerator, new()
    {
        // get all the const string fields
        var trackingNames = typeof(TTrackingNames)
                           .GetFields()
                           .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                           .Select(x => (string?)x.GetRawConstantValue()!)
                           .Where(x => !string.IsNullOrEmpty(x))
                           .ToArray();

        return GetGeneratedTrees<TGenerator>(sources, trackingNames);
    }

    public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedTrees<T>(string[] source, params string[] stages)
        where T : IIncrementalGenerator, new()
    {
        var syntaxTrees = source.Select(static x => CSharpSyntaxTree.ParseText(x, cancellationToken: CancellationToken.None));
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(InlineDataAttribute).Assembly.Location),
            ]);

        var compilation = CSharpCompilation.Create(
            "generator",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var runResult = RunGeneratorAndAssertOutput<T>(compilation, stages);

        return (runResult.Diagnostics, runResult.GeneratedTrees.Select(x => x.ToString()).ToArray());
    }

    private static GeneratorDriverRunResult RunGeneratorAndAssertOutput<T>(CSharpCompilation compilation, string[] trackingNames,
        bool assertOutput = true)
        where T : IIncrementalGenerator, new()
    {
        var generator = new T().AsSourceGenerator();

        var opts = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);

        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator], driverOptions: opts);

        var clone = compilation.Clone();
        // Run twice, once with a clone of the compilation
        // Note that we store the returned drive value, as it contains cached previous outputs
        driver = driver.RunGenerators(compilation, CancellationToken.None);
        var runResult = driver.GetRunResult();

        if (assertOutput)
        {
            // Run with a clone of the compilation
            var runResult2 = driver
                .RunGenerators(clone, CancellationToken.None)
                .GetRunResult();

            AssertRunsEqual(runResult, runResult2, trackingNames);

            // verify the second run only generated cached source outputs
            var outputs = runResult2.Results[0]
                .TrackedOutputSteps
                .SelectMany(x => x.Value) // step executions
                .SelectMany(x => x.Outputs); // execution results

            foreach (var (_, reason) in outputs)
            {
                if (reason is not IncrementalStepRunReason.Cached)
                {
                    Assert.Fail("reason should be only Cached");
                }
            }
        }

        return runResult;
    }

    private static void AssertRunsEqual(GeneratorDriverRunResult runResult1, GeneratorDriverRunResult runResult2, string[] trackingNames)
    {
        // We're given all the tracking names, but not all the stages have necessarily executed so filter
        var trackedSteps1 = GetTrackedSteps(runResult1, trackingNames);
        var trackedSteps2 = GetTrackedSteps(runResult2, trackingNames);

        // These should be the same
        Assert.NotEmpty(trackedSteps1);
        Assert.Equal(trackedSteps1.Count, trackedSteps2.Count);
        // TODO!
        /*trackedSteps1.Should()
                     .NotBeEmpty()
                     .And.HaveSameCount(trackedSteps2)
                     .And.ContainKeys(trackedSteps2.Keys);*/

        foreach (var trackedStep in trackedSteps1)
        {
            var trackingName = trackedStep.Key;
            var runSteps1 = trackedStep.Value;
            var runSteps2 = trackedSteps2[trackingName];
            AssertEqual(runSteps1, runSteps2, trackingName);
        }

        static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(
            GeneratorDriverRunResult runResult, string[] trackingNames) =>
            runResult.Results[0]
                     .TrackedSteps
                     .Where(step => trackingNames.Contains(step.Key, StringComparer.Ordinal))
                     .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Can not be simplified")]
    [SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "<Pending>")]
    private static void AssertEqual(
        ImmutableArray<IncrementalGeneratorRunStep> runSteps1,
        ImmutableArray<IncrementalGeneratorRunStep> runSteps2,
        string stepName)
    {
        Assert.Equal(runSteps1.Length, runSteps2.Length);

        for (var i = 0; i < runSteps1.Length; i++)
        {
            var runStep1 = runSteps1[i];
            var runStep2 = runSteps2[i];

            // The outputs should be equal between different runs
            var outputs1 = runStep1.Outputs.Select(x => x.Value);
            var outputs2 = runStep2.Outputs.Select(x => x.Value);

            Assert.Equal(outputs1, outputs2);
            /*outputs1.Should()
                    .Equal(outputs2, $"because {stepName} should produce cacheable outputs");*/

            // Therefore, on the second run the results should always be cached or unchanged!
            // - Unchanged is when the _input_ has changed, but the output hasn't
            // - Cached is when the the input has not changed, so the cached output is used
            foreach (var (_, reason) in runStep2.Outputs)
            {
                if (reason is not IncrementalStepRunReason.Cached and not IncrementalStepRunReason.Unchanged)
                {
                    Assert.Fail($"{stepName} expected to have reason {nameof(IncrementalStepRunReason.Cached)} or {nameof(IncrementalStepRunReason.Unchanged)}");
                }
            }

            // Make sure we're not using anything we shouldn't
            AssertObjectGraph(runStep1, stepName);
            AssertObjectGraph(runStep2, stepName);
        }

        static void AssertObjectGraph(IncrementalGeneratorRunStep runStep, string stepName)
        {
            //var because = $"{stepName} shouldn't contain banned symbols";
            var visited = new HashSet<object>();

            foreach (var (obj, _) in runStep.Outputs)
            {
                Visit(obj);
            }

            void Visit(object? node)
            {
                if (node is null || !visited.Add(node))
                {
                    return;
                }

                Assert.IsNotType<Compilation>(node);
                Assert.IsNotType<ISymbol>(node);
                Assert.IsNotType<SyntaxNode>(node);
                /*node.Should()
                    .NotBeOfType<Compilation>(because)
                    .And.NotBeOfType<ISymbol>(because)
                    .And.NotBeOfType<SyntaxNode>(because);*/

                var type = node.GetType();
                if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                {
                    return;
                }

                if (node is IEnumerable collection and not string)
                {
                    foreach (var element in collection)
                    {
                        Visit(element);
                    }

                    return;
                }

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var fieldValue = field.GetValue(node);
                    Visit(fieldValue);
                }
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            }
        }
    }
}
