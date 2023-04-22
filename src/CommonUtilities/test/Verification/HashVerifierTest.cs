using System;
using System.IO;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Verification;
using AnakinRaW.CommonUtilities.Verification.Hash;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Verification;

public class HashVerifierTest
{
    private readonly HashVerifier _verifier;
    private readonly Mock<IHashingService> _hashing;
    public HashVerifierTest()
    {
        _hashing = new Mock<IHashingService>();
        var sc = new ServiceCollection();
        sc.AddTransient(_ => _hashing.Object);
        _verifier = new HashVerifier(sc.BuildServiceProvider());
    }

    [Fact]
    public void TestInvalidVerificationContext()
    {
        var stream = new MemoryStream();
        var result = _verifier.Verify(stream, new HashVerificationContext(HashType.MD5, Array.Empty<byte>()));
        Assert.Equal(VerificationResult.InvalidContext, result);
    }

    [Fact]
    public void TestHashTypeNoneAlwaysSucceeds()
    {
        var stream = new MemoryStream();
        
        var result = _verifier.Verify(stream, HashVerificationContext.None);
        Assert.Equal(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerificationFailed()
    {
        var stream = new MemoryStream();

        _hashing.Setup(h => h.GetStreamHash(stream, It.IsAny<HashType>()))
            .Returns(new byte[] { 1 });

        var result = _verifier.Verify(stream, new HashVerificationContext(HashType.MD5, new byte[16]));
        Assert.Equal(VerificationResult.Failed, result);
    }

    [Fact]
    public void TestVerificationSucceeds()
    {
        var stream = new MemoryStream();

        var hash = new byte[16];

        _hashing.Setup(h => h.GetStreamHash(stream, It.IsAny<HashType>()))
            .Returns(hash);

        var result = _verifier.Verify(stream, new HashVerificationContext(HashType.MD5, hash));
        Assert.Equal(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerificationWithException()
    {
        var stream = new MemoryStream();

        var hash = new byte[16];

        _hashing.Setup(h => h.GetStreamHash(stream, It.IsAny<HashType>()))
            .Throws<Exception>();

        var result = _verifier.Verify(stream, new HashVerificationContext(HashType.MD5, hash));
        Assert.Equal(VerificationResult.FromError(null), result);
    }
}