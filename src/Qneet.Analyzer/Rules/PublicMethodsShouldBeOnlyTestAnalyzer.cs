using Qneet.Analyzer.Internals;

namespace Qneet.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PublicMethodsShouldBeOnlyTestAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.PublicMethodsShouldBeOnlyTest,
        title: "Public methods should be only test methods",
        messageFormat: "Public methods should be only test methods - not generic, return void, parameterlees, not abstract",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.PublicMethodsShouldBeOnlyTest));

    private static readonly ImmutableArray<DiagnosticDescriptor> s_supportedDiagnostics = [s_rule];

    private static readonly Action<SymbolAnalysisContext> s_handleAnalyzeToStringExists = AnalyzeSymbol;

    private static readonly ImmutableArray<SymbolKind> s_methodDeclaration = [SymbolKind.NamedType];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => s_supportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        Guard.ThrowIfNull(context);

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(s_handleAnalyzeToStringExists, s_methodDeclaration);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        // Skip non-class types
        if (namedTypeSymbol.TypeKind != TypeKind.Class)
            return;

        // Skip if name doesn't end with "Test"
        if (!namedTypeSymbol.Name.EndsWith("Tests", StringComparison.Ordinal))
            return;

        // Check all public methods
        foreach (var method in namedTypeSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (method.DeclaredAccessibility != Accessibility.Public)
                continue;

            // Report if method has parameters
            if (method.Parameters.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, method.Locations[0], method.Name, namedTypeSymbol.Name));
                continue;
            }

            // Report if method does not return void
            if (method.ReturnType.SpecialType != SpecialType.System_Void)
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, method.Locations[0], method.Name, namedTypeSymbol.Name));
            }
        }
    }
}