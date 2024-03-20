using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Qneet.Generators;
using System.Text;

namespace Qneet;

// see https://github.com/andrewlock/NetEscapades.EnumGenerators/blob/main/src/NetEscapades.EnumGenerators/EnumGenerator.cs

[Generator]
public sealed class DataTestCaseGenerator : IIncrementalGenerator
{
    private const string InlineDataAttribute = "Qneet.InlineDataAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        /*context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "EnumExtensionsAttribute.g.cs", SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)))*/

        var methodsToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                InlineDataAttribute,
                predicate: static (node, _) => IsSyntaxTargetForGeneration(node),
                transform: static (c, ct) => GetMethodToGenerate(c, ct)
            )
            .WithTrackingName(TrackingNames.InitialExtraction)
            .Where(static m => m is not null)
            .WithTrackingName(TrackingNames.RemovingNulls);


        /*IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
            context.CompilationProvider.Combine(classDeclarations.Collect());*/

        //context.RegisterSourceOutput(methodsToGenerate, static (spc, source) => Execute(spc, in source));
        context.RegisterImplementationSourceOutput(methodsToGenerate, static (spc, source) => Execute(spc, in source));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node.IsKind(SyntaxKind.MethodDeclaration); // is MethodDeclarationSyntax m && m.AttributeLists.Count > 0;
    }

    private static MethodToGenerate? GetMethodToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var methodSymbol = context.TargetSymbol as IMethodSymbol;
        if (methodSymbol is null)
        {
            // nothing to do if this type isn't available
            return null;
        }

        ct.ThrowIfCancellationRequested();

        var methodName = methodSymbol.Name;
        var type = methodSymbol.ContainingType;
        var ns = methodSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : methodSymbol.ContainingNamespace.ToString();
        var typeName = type.Name;
        var isTypeStatic = type.IsStatic;
        var isValueType = type.IsValueType;
        var isMethodStatic = methodSymbol.IsStatic;

        /*var methodDeclarationSyntax = context.TargetNode as MethodDeclarationSyntax;
        if (methodDeclarationSyntax == null)
        {
            // nothing to do if this method isn't available
            return null;
        }

        ct.ThrowIfCancellationRequested();

        var methodName = methodDeclarationSyntax.Identifier.Text;
        methodDeclarationSyntax.*/

        return new MethodToGenerate(ns, typeName, methodName, isTypeStatic, isValueType, isMethodStatic);
    }

    /* private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
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
     }*/

    private static void Execute(SourceProductionContext context, in MethodToGenerate? m)
    {
        if (m is null)
        {
            return;
        }
        var methodToGenerate = m.Value;
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(methodToGenerate.Namespace))
        {
            sb.Append("namespace ").Append(methodToGenerate.Namespace).Append(';').AppendLine();
        }

        sb.Append("public ");
        if (methodToGenerate.Modifiers.HasFlag(Modifiers.TypeIsStatic))
        {
            sb.Append("static ");
        }
        sb.Append("partial ");
        if (methodToGenerate.Modifiers.HasFlag(Modifiers.TypeIsValue))
        {
            sb.Append("struct ");
        }
        else
        {
            sb.Append("class ");
        }
        sb.AppendLine(methodToGenerate.TypeName);
        sb.Append('{').AppendLine()
            ;

        sb.Append("\tpublic static void ").Append(methodToGenerate.MethodName).AppendLine("()");
        sb.AppendLine("\t{");
        if (methodToGenerate.Modifiers.HasFlag(Modifiers.TypeIsStatic))
        {
            sb.Append("\t\t").Append(methodToGenerate.MethodName).AppendLine("(1);");
        }
        else
        {
            sb.Append("\t\t").Append("var o = new ").Append(methodToGenerate.TypeName).AppendLine("();");
            sb.Append("\t\t").Append("o.").Append(methodToGenerate.MethodName).AppendLine("(1);");
        }

        sb.AppendLine("\t}");

        sb.Append('}');

        context.AddSource("gen.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}

