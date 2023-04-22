using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Verification.Hash;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Verification;

public class HashVerificationContextTest
{
    [Theory]
    [MemberData(nameof(ValidContextData))]
    public void TestValidateCorrect(HashType type, byte[] data)
    {
        var v = new HashVerificationContext(type, data);
        Assert.True(v.Verify());
    }

    [Theory]
    [MemberData(nameof(InvalidContextData))]
    public void TestInvalidateCorrect(HashType type, byte[] data)
    {
        var v = new HashVerificationContext(type, data);
        Assert.False(v.Verify());
    }

    public static IEnumerable<object[]> ValidContextData()
    {
        yield return new object[] { HashType.None, null };
        yield return new object[] { HashType.None, Array.Empty<byte>() };
        yield return new object[] { HashType.MD5, new byte[16] };
        yield return new object[] { HashType.Sha1, new byte[20] };
        yield return new object[] { HashType.Sha256, new byte[32] };
        yield return new object[] { HashType.Sha384, new byte[48] };
        yield return new object[] { HashType.Sha512, new byte[64] };
    }

    public static IEnumerable<object[]> InvalidContextData()
    {
        yield return new object[] { HashType.None, new byte[1] };
        yield return new object[] { HashType.MD5, new byte[1] };
        yield return new object[] { HashType.Sha1, new byte[1] };
        yield return new object[] { HashType.Sha256, new byte[1] };
        yield return new object[] { HashType.Sha384, new byte[1] };
        yield return new object[] { HashType.Sha512, new byte[1] };
        yield return new object[] { HashType.MD5, Array.Empty<byte>() };
        yield return new object[] { HashType.Sha1, Array.Empty<byte>() };
        yield return new object[] { HashType.Sha256, Array.Empty<byte>() };
        yield return new object[] { HashType.Sha384, Array.Empty<byte>() };
        yield return new object[] { HashType.Sha512, Array.Empty<byte>() };
        yield return new object[] { HashType.MD5, null };
        yield return new object[] { HashType.Sha1, null };
        yield return new object[] { HashType.Sha256, null };
        yield return new object[] { HashType.Sha384, null };
        yield return new object[] { HashType.Sha512, null };
    }

    [Fact]
    public void FromHash_Null_ReturnsNone()
    {
        var result = HashVerificationContext.FromHash(null);
        Assert.Equal(HashType.None, result.HashType);
    }

    [Fact]
    public void FromHash_Empty_ReturnsNone()
    {
        var hash = Array.Empty<byte>();
        var result = HashVerificationContext.FromHash(hash);
        Assert.Equal(HashType.None, result.HashType);
    }

    [Theory]
    [InlineData(HashType.MD5)]
    [InlineData(HashType.Sha1)]
    [InlineData(HashType.Sha256)]
    [InlineData(HashType.Sha384)]
    [InlineData(HashType.Sha512)]
    public void FromHash_ValidHash_ReturnsExpectedResult(HashType hashType)
    {
        var hash = GenerateRandomHash(hashType);

        var result = HashVerificationContext.FromHash(hash);

        Assert.Equal(hashType, result.HashType);
        Assert.Equal(hash, result.Hash);
    }

    [Fact]
    public void FromHash_UnknownHashLength_ThrowsArgumentException()
    {
        var hash = new byte[] { 0x11, 0x22, 0x33 };
        Assert.Throws<ArgumentException>(() => HashVerificationContext.FromHash(hash));
    }

    private static byte[] GenerateRandomHash(HashType hashType)
    {
        var hashSize = (int)hashType;
        var hash = new byte[hashSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(hash);
        return hash;
    }
}