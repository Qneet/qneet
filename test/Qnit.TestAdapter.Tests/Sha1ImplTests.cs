using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Qnit.TestAdapter.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
public static class Sha1ImplTests
{
    private static readonly char[] s_lookupTableLower =
        ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'];

    public static void SHA1_TestVectors_EmptyString()
    {
        SHA1_TestVector(
            string.Empty,
            "da39a3ee5e6b4b0d3255bfef95601890afd80709"
        );
    }

    public static void SHA1_TestVectors_abc()
    {
        SHA1_TestVector(
            "abc",
            "a9993e364706816aba3e25717850c26c9cd0d89d"
        );
    }

    public static void SHA1_TestVectors_448Bits()
    {
        SHA1_TestVector(
            "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
            "84983e441c3bd26ebaae4aa1f95129e5e54670f1"
        );
    }

    public static void SHA1_TestVectors_896Bits()
    {
        SHA1_TestVector(
            "abcdefghbcdefghicdefghijdefghijkefghijklfghijklmghijklmnhijklmnoijklmnopjklmnopqklmnopqrlmnopqrsmnopqrstnopqrstu",
            "a49b2446a02c645bf419f995b67091253a04a259"
        );
    }

    public static void SHA1_TestVectors_1Block()
    {
        SHA1_TestRepetitionVector(
            'a',
            512 / 8
        );
    }

    public static void SHA1_ExtremelyLarge_TestVectors_500k_a()
    {
        SHA1_TestRepetitionVector(
            'a',
            500_000
        );
    }

    public static void SHA1_ExtremelyLarge_TestVectors_900k_a()
    {
        SHA1_TestRepetitionVector(
            'a',
            900_000
        );
    }

    public static void SHA1_ExtremelyLarge_TestVectors_999999_a()
    {
        SHA1_TestRepetitionVector(
            'a',
            999_999
        );
    }

    public static void SHA1_ExtremelyLarge_TestVectors_1M_a()
    {
        SHA1_TestRepetitionVector(
            'a',
            1_000_000,
            "34aa973cd4c4daa4f61eeb2bdbad27316534016f"
        );
    }

    public static void SHA1_ExtremelyLarge_TestVectors_10M_a()
    {
        SHA1_TestRepetitionVector(
            'a',
            10_000_000
        );
    }

    private static void SHA1_TestVector(string message, string expected)
    {
        // Arrange
        var shaHasher1 = new Sha1Implementation();

        // Act
        var bytes = Encoding.UTF8.GetBytes(message);
        Span<byte> hash = stackalloc byte[Sha1Implementation.DigestBytes];
        shaHasher1.ComputeHash(bytes, hash);
        var digest1 = ToHex(hash);

        // Assert
        Assert.Equal(expected, digest1);
    }

    private static void SHA1_TestRepetitionVector(char input, int repetition, string? expected = null)
    {
        // Arrange
        var shaHasher1 = new Sha1Implementation();
        var shaHasher2 = new Sha1Implementation();

        var bytes = GC.AllocateUninitializedArray<byte>(repetition).AsSpan();
        Unsafe.InitBlock(ref MemoryMarshal.GetReference(bytes), (byte)input, (uint)repetition);

        if (string.IsNullOrEmpty(expected))
        {
#pragma warning disable CA5350
            expected = ToHex(SHA1.HashData(bytes));
#pragma warning restore CA5350
        }

        // Act
        Span<byte> hash1 = stackalloc byte[Sha1Implementation.DigestBytes];
        shaHasher1.ComputeHash(bytes, hash1);
        var digest1 = ToHex(hash1);
        var blocks = bytes.Length / Sha1Implementation.BlockBytes;
        for (var i = 0; i < blocks; i += 1)
        {
            shaHasher2.ProcessBlock(bytes.Slice(0, Sha1Implementation.BlockBytes));
            bytes = bytes.Slice(Sha1Implementation.BlockBytes);
        }

        if (bytes.Length != 0)
        {
            shaHasher2.ProcessBlock(bytes);
        }

        Span<byte> hash2 = stackalloc byte[Sha1Implementation.DigestBytes];
        shaHasher2.ProcessFinalBlock(hash2);
        var digest2 = ToHex(hash2);

        // Assert
        Assert.Equal(expected, digest1);
        Assert.Equal(expected, digest2);
    }

    private static string ToHex(ReadOnlySpan<byte> hash)
    {
        var length1 = hash.Length * 2;
        Span<char> chArray = stackalloc char[length1];
        for (var j = 0; j < hash.Length; j++)
        {
            var v = hash[j] & 0xFF;
            chArray[j << 1] = s_lookupTableLower[v >> 4];
            chArray[(j << 1) + 1] = s_lookupTableLower[v & 0x0F];
        }

        var result = new string(chArray);
        return result;
    }
}

