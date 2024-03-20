namespace Qneet.TestAdapter;
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Test case name is mandatory")]
public sealed class TestCaseNotFoundException : QneetException
{
    public TestCaseNotFoundException(string testCaseName) : base($"Test case \"{testCaseName}\" not found")
    {
    }

    public TestCaseNotFoundException(string testCaseName, Exception innerException) : base($"Test case \"{testCaseName}\" not found", innerException)
    {
    }
}
