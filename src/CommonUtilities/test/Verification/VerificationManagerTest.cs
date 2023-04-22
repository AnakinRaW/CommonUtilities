using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using AnakinRaW.CommonUtilities.Verification;
using AnakinRaW.CommonUtilities.Verification.Empty;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Verification;

public class VerificationManagerTest
{
    private readonly VerificationManager _manager;

    public VerificationManagerTest()
    {
        _manager = new VerificationManager(new ServiceCollection().BuildServiceProvider());
    }

    [Fact]
    public void RegisterVerifiers()
    {
        Assert.Throws<ArgumentNullException>(() => _manager.RegisterVerifier<IVerificationContext>(null!));

        var verifierA = new Mock<IVerifier<IVerificationContext>>();
        var verifierB = new Mock<IVerifier<EmptyVerificationContext>>();
        var verifierC = new Mock<IVerifier<EmptyVerificationContext>>();

        Assert.Empty(_manager.Verifiers);

        _manager.RegisterVerifier(verifierA.Object);
        var pairA = Assert.Single(_manager.Verifiers);
        Assert.Equal(typeof(IVerificationContext), pairA.Key);
        var a = Assert.Single(pairA.Value);
        Assert.Equal(verifierA.Object, a);


        _manager.RegisterVerifier(verifierB.Object);
        _manager.RegisterVerifier(verifierC.Object);
        Assert.Equal(2, _manager.Verifiers.Count);

        var verifiers = _manager.Verifiers[typeof(EmptyVerificationContext)];
        Assert.Equal(2, verifiers.Count);
        Assert.Contains(verifierB.Object, verifiers);
        Assert.Contains(verifierC.Object, verifiers);
    }

    [Fact]
    public void RemoveVerifiers()
    {
        Assert.Throws<ArgumentNullException>(() => _manager.RemoveVerifier(null!));

        var verifierA = new Mock<IVerifier>();
        var verifierB = new Mock<IVerifier>();
        var verifierC = new Mock<IVerifier>();
        _manager.Verifiers[typeof(IVerificationContext)] = new List<IVerifier> { verifierA.Object, verifierA.Object };
        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        _manager.RemoveVerifier(verifierC.Object);
        Assert.Equal(4, _manager.Verifiers.Values.Sum(x => x.Count));

        _manager.RemoveVerifier(verifierA.Object);
        Assert.Equal(1, _manager.Verifiers.Values.Sum(x => x.Count));
    }

    [Fact]
    public void TestVerifyFails()
    {
        Assert.Throws<ArgumentNullException>(() => _manager.Verify((Stream)null, Mock.Of<IVerificationContext>()));
        Assert.Throws<ArgumentNullException>(() => _manager.Verify((IFileInfo)null, Mock.Of<IVerificationContext>()));
    }

    [Fact]
    public void TestGetVerifiers()
    {
        var verifierA = new Mock<IVerifier>();
        var verifierB = new Mock<IVerifier>();
        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        Assert.Null(_manager.GetVerifier(typeof(IVerificationContext)));
        Assert.Equal(2, _manager.GetVerifier(typeof(EmptyVerificationContext))!.Count);
    }

    [Fact]
    public void TestVerifyNoVerifiers()
    {
        var context = Mock.Of<IVerificationContext>();

        var verifier = new Mock<IVerifier>();
        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers[typeof(IVerificationContext)] = new List<IVerifier> { verifier.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.Equal(VerificationResult.NotVerified, result);
    }

    [Fact]
    public void TestVerifyException()
    {
        var context = new EmptyVerificationContext();

        var verifier = new Mock<IVerifier<EmptyVerificationContext>>();

        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), It.IsAny<IVerificationContext>())).Throws<Exception>();

        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifier.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.Equal(VerificationResult.FromError(null), result);
    }

    [Fact]
    public void TestVerifySuccess()
    {
        var context = new EmptyVerificationContext();

        var verifier = new Mock<IVerifier>();
        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifier.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.Equal(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerifySuccessMany()
    {
        var context = new EmptyVerificationContext();

        var verifierA = new Mock<IVerifier>();
        verifierA.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);
        var verifierB = new Mock<IVerifier>();
        verifierB.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.Equal(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerifyFailure()
    {
        var context = new EmptyVerificationContext();

        var verifier = new Mock<IVerifier>();
        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Failed);

        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifier.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.Equal(VerificationResult.Failed, result);
    }

    [Fact]
    public void TestVerifyInvalid()
    {
        var context = new EmptyVerificationContext();

        var verifier = new Mock<IVerifier>();
        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.InvalidContext);

        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifier.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.Equal(VerificationResult.InvalidContext, result);
    }

    [Fact]
    public void TestVerifyFailureMany1()
    {
        var context = new EmptyVerificationContext();

        var verifierA = new Mock<IVerifier>();
        verifierA.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);
        var verifierB = new Mock<IVerifier>();
        verifierB.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Failed);

        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.NotEqual(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerifyFailureMany2()
    {
        var context = new EmptyVerificationContext();

        var verifierA = new Mock<IVerifier>();
        verifierA.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Failed);
        var verifierB = new Mock<IVerifier>();
        verifierB.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.NotEqual(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerifyFailureMany3()
    {
        var context = new EmptyVerificationContext();

        var verifierA = new Mock<IVerifier>();
        verifierA.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.InvalidContext);
        var verifierB = new Mock<IVerifier>();
        verifierB.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.NotEqual(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerifyManySuccess()
    {
        var context = new EmptyVerificationContext();

        var verifierA = new Mock<IVerifier>();
        verifierA.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);
        var verifierB = new Mock<IVerifier>();
        verifierB.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers[typeof(EmptyVerificationContext)] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        var result = _manager.Verify(new MemoryStream(), context);
        Assert.Equal(VerificationResult.Success, result);
    }
}