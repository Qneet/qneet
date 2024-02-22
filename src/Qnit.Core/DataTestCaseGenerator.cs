using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Qnit;

[Generator]
public sealed class DataTestCaseGenerator : IIncrementalGenerator
{
    private const string InlineDataAttribute = "Qnit.InlineDataAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(static (s, _) => IsSyntaxTargetForGeneration(s),
                static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
            context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0;

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in methodDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                if (attributeSymbol == null)
                {
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (string.Equals(fullName, InlineDataAttribute, StringComparison.Ordinal))
                {
                    return methodDeclarationSyntax.Parent as ClassDeclarationSyntax;
                }
            }
        }

        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        var distinctClasses = classes.Distinct();

        /*var p = new Parser(compilation, context.ReportDiagnostic, context.CancellationToken);
        IReadOnlyList<LoggerClass> logClasses = p.GetLogClasses(distinctClasses);
        if (logClasses.Count > 0)
        {
            var e = new Emitter();
            string result = e.Emit(logClasses, context.CancellationToken);

            context.AddSource("LoggerMessage.g.cs", SourceText.From(result, Encoding.UTF8));
        }*/
    }
}

