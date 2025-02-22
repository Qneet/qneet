namespace Qneet.TestAdapter.Tests;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "It`s tests")]
[SuppressMessage("Minor Code Smell", "S3400:Methods should not return constants", Justification = "It`s tests")]
[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "<Pending>")]
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "It's test")]
public class SomeClassTests
{
    public int M() => 1;
}
