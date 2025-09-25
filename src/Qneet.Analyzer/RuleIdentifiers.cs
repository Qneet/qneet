namespace Qneet.Analyzer;

internal static class RuleIdentifiers
{
    public const string PublicMethodsShouldBeOnlyTest = "QNEET0001";

    public static string GetHelpUri(string identifier)
    {
        return $"https://github.com/Qneet/qneet/blob/main/docs/Rules/{identifier}.md";
    }
}