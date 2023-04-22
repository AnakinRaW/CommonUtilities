using System;
using System.IO;
using AnakinRaW.CommonUtilities.Verification;
using AnakinRaW.CommonUtilities.Verification.Empty;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Verification;

public class VerifierBaseTests
{
    [Fact]
    public void Verify_ShouldReturnNotVerified_WhenVerificationContextIsNull()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var verifier = new Mock<VerifierBase<IVerificationContext>>(serviceProviderMock.Object) { CallBase = true };
        var data = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => verifier.Object.Verify(data, null));
        Assert.Throws<ArgumentNullException>(() => verifier.Object.Verify(null!, new EmptyVerificationContext()));
    }

    [Fact]
    public void InvalidContext()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var verifier = new Mock<VerifierBase<IVerificationContext>>(serviceProviderMock.Object) { CallBase = true };
       
        var data = new MemoryStream();
        var verificationContextMock = new Mock<IVerificationContext>();
        verificationContextMock.Setup(v => v.Verify()).Returns(false);

        var result = verifier.Object.Verify(data, verificationContextMock.Object);
        Assert.Equal(VerificationResultStatus.VerificationContextError, result.Status);
    }

    [Fact]
    public void VerifyCoreThrows()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var verifier = new Mock<VerifierBase<IVerificationContext>>(serviceProviderMock.Object) { CallBase = true };
        var data = new MemoryStream();
        var verificationContextMock = new Mock<IVerificationContext>();
        verificationContextMock.Setup(v => v.Verify()).Returns(true);

        verifier.Protected().Setup<VerificationResult>("VerifyCore", data, verificationContextMock.Object)
            .Throws(new Exception());


        var result = verifier.Object.Verify(data, verificationContextMock.Object);

        Assert.Equal(VerificationResultStatus.Exception, result.Status);
        verifier.Protected().Verify<VerificationResult>("VerifyCore", Times.Once(), data, verificationContextMock.Object);
    }

    [Fact]
    public void VerifyCoreWithSuccess()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var verifier = new Mock<VerifierBase<IVerificationContext>>(serviceProviderMock.Object) { CallBase = true };
        var data = new MemoryStream();
        var verificationContextMock = new Mock<IVerificationContext>();
        verificationContextMock.Setup(v => v.Verify()).Returns(true);

        verifier.Protected().Setup<VerificationResult>("VerifyCore", data, verificationContextMock.Object)
            .Returns(VerificationResult.Success);

        var result = verifier.Object.Verify(data, verificationContextMock.Object);
        Assert.Equal(VerificationResultStatus.Success, result.Status);
    }

    [Fact]
    public void VerifyCoreWithFailure()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var verifier = new Mock<VerifierBase<IVerificationContext>>(serviceProviderMock.Object) { CallBase = true };
        var data = new MemoryStream();
        var verificationContextMock = new Mock<IVerificationContext>();
        verificationContextMock.Setup(v => v.Verify()).Returns(true);

        verifier.Protected().Setup<VerificationResult>("VerifyCore", data, verificationContextMock.Object)
            .Returns(VerificationResult.Failed);

        var result = verifier.Object.Verify(data, verificationContextMock.Object);
        Assert.Equal(VerificationResultStatus.VerificationFailed, result.Status);
    }

    [Fact]
    public void VerifyWithCast()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var verifier = new Mock<VerifierBase<EmptyVerificationContext>>(serviceProviderMock.Object) { CallBase = true };
        var data = new MemoryStream();

        var context = new EmptyVerificationContext();
        ((IVerifier) verifier.Object).Verify(data, context);
        verifier.Protected().Verify<VerificationResult>("VerifyCore", Times.Once(), data, context);
    }

    [Fact]
    public void VerifyWithInvalidCast()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var verifier = new Mock<VerifierBase<TestContext>>(serviceProviderMock.Object) { CallBase = true };
        var data = new MemoryStream();

        var context = new EmptyVerificationContext();
        Assert.Throws<InvalidCastException>(() => ((IVerifier)verifier.Object).Verify(data, context));
    }

    public struct TestContext : IVerificationContext
    {
        bool IVerificationContext.Verify()
        {
            return true;
        }
    }
}