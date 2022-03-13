using Xunit;

namespace Qnit.TestAdapter.Tests;

public static class TestIdProviderTests
{
    public static void IdCompatibilityTests1()
    {
        IdCompatibilityTests(new[] { "eea339da-6b5e-0d4b-3255-bfef95601890", "" });
    }

    public static void IdCompatibilityTests2()
    {
        IdCompatibilityTests(new[] { "740b9afc-3350-4257-ca01-5bd47799147d", "adapter://", "name1" });
    }

    public static void IdCompatibilityTests3()
    {
        IdCompatibilityTests(new[] { "119c5b31-c0fb-1c12-6d1a-d617bb2bd996", "adapter://namesamplenam.testname" });
    }

    public static void IdCompatibilityTests4()
    {
        IdCompatibilityTests(new[] { "2a4c33ec-6115-4bd7-2e94-71f2fd3a5ee3", "adapter://namesamplenamespace.testname" });
    }

    public static void IdCompatibilityTests5()
    {
        IdCompatibilityTests(new[] { "119c5b31-c0fb-1c12-6d1a-d617bb2bd996", "adapter://", "name", "samplenam", ".", "testname" });
    }

    public static void IdCompatibilityTests6()
    {
        IdCompatibilityTests(new[] { "2a4c33ec-6115-4bd7-2e94-71f2fd3a5ee3", "adapter://", "name", "samplenamespace", ".", "testname" });
    }

    public static void IdCompatibilityTests7()
    {
        IdCompatibilityTests(new[] { "1fc07043-3d2d-1401-c732-3b507feec548", "adapter://namesamplenam.testnameaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" });
    }

    public static void IdCompatibilityTests8()
    {
        IdCompatibilityTests(new[] { "24e8a50b-2766-6a12-f461-9f8e4fa1cbb5", "adapter://namesamplenamespace.testnameaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" });
    }

    public static void IdCompatibilityTests9()
    {
        IdCompatibilityTests(new[] { "1fc07043-3d2d-1401-c732-3b507feec548", "adapter://", "name", "samplenam", ".", "testname", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" });
    }

    public static void IdCompatibilityTests10()
    {
        IdCompatibilityTests(new[] { "24e8a50b-2766-6a12-f461-9f8e4fa1cbb5", "adapter://", "name", "samplenamespace", ".", "testname", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" });
    }

    private static void IdCompatibilityTests(string[] data)
    {
        // Arrange
        var expectedId = new Guid(data[0]);

        // Act
        var idProvider = new TestIdProvider();
        foreach (var d in data.Skip(1))
        {
            idProvider.Append(d);
        }

        var id = idProvider.GetId();

        // Assert
        Assert.Equal(expectedId, id);
    }

    public static void IdGeneration_TestVectors_EmptyString()
    {
        IdGeneration_TestVector(
            string.Empty,
            "eea339da-6b5e-0d4b-3255-bfef95601890"
        );
    }


    public static void IdGeneration_TestVectors_abc()
    {
        IdGeneration_TestVector(
            "abc",
            "1af4049f-8584-1614-2050-e3d68c1a7abb"
        );
    }

    public static void IdGeneration_TestVectors_448Bits()
    {
        IdGeneration_TestVector(
            "abcdbcdecdefdefgefghfghighij",
            "7610f6db-8808-4bb7-b076-96871a96329c"
        );
    }

    public static void IdGeneration_TestVectors_896Bits()
    {
        IdGeneration_TestVector(
            "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
            "76d8d751-c79a-402c-9c5b-0e3f69c60adc"
        );
    }

    public static void IdGeneration_TestVectors_1Block()
    {
        IdGeneration_TestRepetitionVector(
            "a", 512 / 16,
            "99b1aec7-ff50-5229-a378-70ca37914c90"
        );
    }

    public static void IdGeneration_ExtremelyLarge_TestVectors_100k_abc()
    {
        IdGeneration_TestRepetitionVector(
            "abc", 100_000,
            "11dbfc20-b34a-eef6-158e-ea8c201dfff9"
        );
    }

    public static void IdGeneration_ExtremelyLarge_TestVectors_10M_abc()
    {
        IdGeneration_TestRepetitionVector(
            "abc", 10_000_000,
            "78640f07-8041-71bd-6461-3a7e4db52389"
        );
    }

    private static void IdGeneration_TestVector(string testName, string expected)
    {
        // Arrange
#pragma warning disable CA1308 // Normalize strings to uppercase
        expected = expected.Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
        var idProvider = new TestIdProvider();

        // Act
        idProvider.Append(testName);
        var actual = idProvider.GetId().ToString();

        // Assert
        Assert.Equal(expected, actual);
    }

    private static void IdGeneration_TestRepetitionVector(string input, int repetition, string expected)
    {
        // Arrange
        var idProvider = new TestIdProvider();

        // Act
        for (var i = 0; i < repetition; i++)
        {
            idProvider.Append(input);
        }

        var id = idProvider.GetId().ToString();

        // Assert
        Assert.Equal(expected, id);
    }
}