namespace Qneet.TestAdapter;
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Should not have default constructor")]
public abstract class QneetException : Exception
{
    protected QneetException(string message) : base(message)
    {
    }

    protected QneetException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
