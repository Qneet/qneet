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
#pragma warning disable S1481 // Unused local variables should be removed
        var (diagnostics, output) = TestHelpers.GetGeneratedOutput<DataTestCaseGenerator>(input);
#pragma warning restore S1481 // Unused local variables should be removed

        Assert.Empty(diagnostics);
    }
}
