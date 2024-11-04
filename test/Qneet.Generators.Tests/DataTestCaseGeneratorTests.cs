using Xunit;

namespace Qneet.Generators.Tests;

public static class DataTestCaseGeneratorTests
{
    [SuppressMessage("Usage", "MA0136:Raw String contains an implicit end of line character", Justification = "<Pending>")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public static void Test()
    {
        const string input = """
            using Qneet;
            using System;

            public partial sealed class Tests : IDisposable
            {
                [InlineData(1)]
                public void Test(int a) => throw new NotImplementedException();
            }
            """;
        var (diagnostics, _) = TestHelpers.GetGeneratedOutput<DataTestCaseGenerator>(input);

        Assert.Empty(diagnostics);
    }
}
