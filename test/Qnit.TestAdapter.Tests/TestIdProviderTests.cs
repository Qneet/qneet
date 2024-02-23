using System.Text;
using Xunit;

namespace Qnit.TestAdapter.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can have underscore")]
public static class TestIdProviderTests
{
    public static void IdCompatibilityTests1()
    {
        IdCompatibilityTests(["eea339da-6b5e-0d4b-3255-bfef95601890", ""]);
    }

    public static void IdCompatibilityTests2()
    {
        IdCompatibilityTests(["740b9afc-3350-4257-ca01-5bd47799147d", "adapter://", "name1"]);
    }

    public static void IdCompatibilityTests3()
    {
        IdCompatibilityTests(["119c5b31-c0fb-1c12-6d1a-d617bb2bd996", "adapter://namesamplenam.testname"]);
    }

    public static void IdCompatibilityTests4()
    {
        IdCompatibilityTests(["2a4c33ec-6115-4bd7-2e94-71f2fd3a5ee3", "adapter://namesamplenamespace.testname"]);
    }

    public static void IdCompatibilityTests5()
    {
        IdCompatibilityTests(["119c5b31-c0fb-1c12-6d1a-d617bb2bd996", "adapter://", "name", "samplenam", ".", "testname"]);
    }

    public static void IdCompatibilityTests6()
    {
        IdCompatibilityTests(["2a4c33ec-6115-4bd7-2e94-71f2fd3a5ee3", "adapter://", "name", "samplenamespace", ".", "testname"]);
    }

    public static void IdCompatibilityTests7()
    {
        IdCompatibilityTests(["1fc07043-3d2d-1401-c732-3b507feec548", "adapter://namesamplenam.testnameaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]);
    }

    public static void IdCompatibilityTests8()
    {
        IdCompatibilityTests(["24e8a50b-2766-6a12-f461-9f8e4fa1cbb5", "adapter://namesamplenamespace.testnameaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]);
    }

    public static void IdCompatibilityTests9()
    {
        IdCompatibilityTests(["1fc07043-3d2d-1401-c732-3b507feec548", "adapter://", "name", "samplenam", ".", "testname", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]);
    }

    public static void IdCompatibilityTests10()
    {
        IdCompatibilityTests(["24e8a50b-2766-6a12-f461-9f8e4fa1cbb5", "adapter://", "name", "samplenamespace", ".", "testname", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]);
    }

    private static void IdCompatibilityTests(string[] data)
    {
        // Arrange
        var expectedId = new Guid(data[0]);

        // Act
        var idProvider = CreateTestIdProvider();
        for (var i = 1; i < data.Length; i++)
        {
            idProvider.Append(data[i]);
        }

        var id = idProvider.GetIdAndReset();

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
        var idProvider = CreateTestIdProvider();

        // Act
        idProvider.Append(testName);
        var actual = idProvider.GetIdAndReset().ToString();

        // Assert
        Assert.Equal(expected, actual);
    }

    private static void IdGeneration_TestRepetitionVector(string input, int repetition, string expected)
    {
        // Arrange
        var idProvider = CreateTestIdProvider();

        // Act
        ReadOnlySpan<byte> buffer = Encoding.Unicode.GetBytes(input).AsSpan();
        for (var i = 0; i < repetition; i++)
        {
            idProvider.Append(buffer);
        }

        var id = idProvider.GetIdAndReset().ToString();

        // Assert
        Assert.Equal(expected, id);
    }

    private static TestIdProvider CreateTestIdProvider() => new();
}
