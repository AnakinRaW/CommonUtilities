using System;
using System.Collections.Generic;
using Sklavenwalker.CommonUtilities.DownloadManager.Verification;
using Sklavenwalker.CommonUtilities.Hashing;
using Xunit;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Test.Verification;

public class VerificationContextTest
{
    [Fact]
    public void TestCtor()
    {
        Assert.Throws<ArgumentNullException>(() => new VerificationContext(null, HashType.None));
    }

    [Theory]
    [MemberData(nameof(ValidContextData))]
    public void TestValidateCorrect(HashType type, byte[] data)
    {
        var v = new VerificationContext(data, type);
        Assert.True(v.Verify());
    }

    [Theory]
    [MemberData(nameof(InvalidContextData))]
    public void TestInvalidateCorrect(HashType type, byte[] data)
    {
        var v = new VerificationContext(data, type);
        Assert.False(v.Verify());
    }

    public static IEnumerable<object[]> ValidContextData()
    {
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
    }
}