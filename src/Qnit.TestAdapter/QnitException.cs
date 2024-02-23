namespace Qnit.TestAdapter;
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Should not have default constructor")]
public abstract class QnitException : Exception
{
    protected QnitException(string message) : base(message)
    {
    }

    protected QnitException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
