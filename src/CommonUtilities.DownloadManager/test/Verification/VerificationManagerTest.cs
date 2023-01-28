using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AnakinRaW.CommonUtilities.DownloadManager.Verification;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Verification;

public class VerificationManagerTest
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly VerificationManager _manager;

    public VerificationManagerTest()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(_fileSystem);
        _manager = new VerificationManager(sc.BuildServiceProvider());
    }

    [Fact]
    public void RegisterTest()
    {
        Assert.Throws<ArgumentNullException>(() => _manager.RegisterVerifier(null, new Mock<IVerifier>().Object));
        Assert.Throws<ArgumentException>(() => _manager.RegisterVerifier("", new Mock<IVerifier>().Object));
        Assert.Throws<ArgumentException>(() => _manager.RegisterVerifier("  ", new Mock<IVerifier>().Object));
        Assert.Throws<ArgumentNullException>(() => _manager.RegisterVerifier("exe", null));

        Assert.Empty(_manager.Verifiers);
        var verifierA = new Mock<IVerifier>();
        _manager.RegisterVerifier("exe", verifierA.Object);
        var pairA = Assert.Single(_manager.Verifiers);
        Assert.Equal((string)"exe", (string)pairA.Key);
        var a = Assert.Single(pairA.Value);
        Assert.Equal(verifierA.Object, a);

        _manager.RegisterVerifier(".exe", verifierA.Object);
        pairA = Assert.Single(_manager.Verifiers);
        Assert.Equal(2, pairA.Value.Count);

        _manager.RegisterVerifier("ExE", verifierA.Object);
        pairA = Assert.Single(_manager.Verifiers);
        Assert.Equal(3, pairA.Value.Count);

        _manager.RegisterVerifier("dll", verifierA.Object);
        Assert.Equal(2, _manager.Verifiers.Count);
    }

    [Fact]
    public void RemoveTest()
    {
        Assert.Throws<ArgumentNullException>(() => _manager.RemoveVerifier(null, new Mock<IVerifier>().Object));
        Assert.Throws<ArgumentException>(() => _manager.RemoveVerifier("", new Mock<IVerifier>().Object));
        Assert.Throws<ArgumentException>(() => _manager.RemoveVerifier("  ", new Mock<IVerifier>().Object));
        Assert.Throws<ArgumentNullException>(() => _manager.RemoveVerifier("exe", null));

        var verifierA = new Mock<IVerifier>();
        var verifierB = new Mock<IVerifier>();
        _manager.Verifiers["exe"] = new List<IVerifier> { verifierA.Object, verifierA.Object };
        _manager.Verifiers["dll"] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        _manager.RemoveVerifier("exe", verifierB.Object);
        Assert.Equal(2, _manager.Verifiers.Count);
        Assert.Equal(2, _manager.Verifiers["exe"].Count);

        _manager.RemoveVerifier(".ExE", verifierA.Object);
        Assert.Equal(1, _manager.Verifiers.Count);
        Assert.False((bool)_manager.Verifiers.ContainsKey("exe"));

        _manager.RemoveVerifier("dll", verifierA.Object);
        Assert.Equal(1, _manager.Verifiers.Count);
        Assert.Single(_manager.Verifiers["dll"]);

        _manager.RemoveVerifier("dll", verifierB.Object);
        Assert.Empty(_manager.Verifiers);
    }

    [Fact]
    public void TestVerifyFails()
    {
        Assert.Throws<ArgumentNullException>(() => _manager.Verify((Stream) null, new VerificationContext(Array.Empty<byte>(), HashType.None)));
        Assert.Throws<ArgumentNullException>(() => _manager.Verify((IFileInfo) null, new VerificationContext(Array.Empty<byte>(), HashType.None)));
        Assert.Throws<ArgumentException>(() => _manager.Verify(new MemoryStream(), new VerificationContext(Array.Empty<byte>(), HashType.None)));
        
        Assert.Throws<FileNotFoundException>(() => _manager.Verify(new FileStream("test.file", FileMode.Open), new VerificationContext(Array.Empty<byte>(), HashType.None)));
        Assert.Throws<FileNotFoundException>(() => _manager.Verify(_fileSystem.FileInfo.New("test.file"), new VerificationContext(Array.Empty<byte>(), HashType.None)));
    }

    [Fact]
    public void TestVerifyNoVerifiers()
    { 
        _fileSystem.AddFile("test.file", new MockFileData(string.Empty));
        var file = _fileSystem.FileInfo.New("test.file");

        var context = new VerificationContext(Array.Empty<byte>(), HashType.None);

        var verifier = new Mock<IVerifier>();
        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers["exe"] = new List<IVerifier> { verifier.Object };

        var result = _manager.Verify(file, new VerificationContext(Array.Empty<byte>(), HashType.None));
        Assert.Equal(VerificationResult.NotVerified, result);
    }

    [Fact]
    public void TestVerifyException()
    {
        _fileSystem.AddFile("test.file", new MockFileData(string.Empty));
        var file = _fileSystem.FileInfo.New("test.file");

        var context = new VerificationContext(Array.Empty<byte>(), HashType.None);

        var verifier = new Mock<IVerifier>();
        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Throws<Exception>();

        _manager.Verifiers["file"] = new List<IVerifier> { verifier.Object };

        var result = _manager.Verify(file, context);
        Assert.Equal(VerificationResult.Exception, result);
    }

    [Fact]
    public void TestVerifySuccess()
    {
        _fileSystem.AddFile("test.file", new MockFileData(string.Empty));
        var file = _fileSystem.FileInfo.New("test.file");

        var context = new VerificationContext(Array.Empty<byte>(), HashType.None);

        var verifier = new Mock<IVerifier>();
        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers["file"] = new List<IVerifier> { verifier.Object };
        
        var result = _manager.Verify(file, context);
        Assert.Equal(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerifySuccessMany()
    {
        _fileSystem.AddFile("test.file", new MockFileData(string.Empty));
        var file = _fileSystem.FileInfo.New("test.file");

        var context = new VerificationContext(Array.Empty<byte>(), HashType.None);

        var verifierA = new Mock<IVerifier>();
        verifierA.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);
        var verifierB = new Mock<IVerifier>();
        verifierB.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers["file"] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        var result = _manager.Verify(file, context);
        Assert.Equal(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerifyFailure()
    {
        _fileSystem.AddFile("test.file", new MockFileData(string.Empty));
        var file = _fileSystem.FileInfo.New("test.file");

        var context = new VerificationContext(Array.Empty<byte>(), HashType.None);

        var verifier = new Mock<IVerifier>();
        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.VerificationFailed);

        _manager.Verifiers["file"] = new List<IVerifier> { verifier.Object };

        var result = _manager.Verify(file, context);
        Assert.Equal(VerificationResult.VerificationFailed, result);
    }

    [Fact]
    public void TestVerifyInvalid()
    {
        _fileSystem.AddFile("test.file", new MockFileData(string.Empty));
        var file = _fileSystem.FileInfo.New("test.file");

        var context = new VerificationContext(Array.Empty<byte>(), HashType.None);

        var verifier = new Mock<IVerifier>();
        verifier.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.VerificationContextError);

        _manager.Verifiers["file"] = new List<IVerifier> { verifier.Object };

        var result = _manager.Verify(file, context);
        Assert.Equal(VerificationResult.VerificationContextError, result);
    }

    [Fact]
    public void TestVerifyFailureMany1()
    {
        _fileSystem.AddFile("test.file", new MockFileData(string.Empty));
        var file = _fileSystem.FileInfo.New("test.file");

        var context = new VerificationContext(Array.Empty<byte>(), HashType.None);

        var verifierA = new Mock<IVerifier>();
        verifierA.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);
        var verifierB = new Mock<IVerifier>();
        verifierB.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.VerificationFailed);

        _manager.Verifiers["file"] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        var result = _manager.Verify(file, context);
        Assert.Equal(VerificationResult.VerificationFailed, result);
    }

    [Fact]
    public void TestVerifyFailureMany2()
    {
        _fileSystem.AddFile("test.file", new MockFileData(string.Empty));
        var file = _fileSystem.FileInfo.New("test.file");

        var context = new VerificationContext(Array.Empty<byte>(), HashType.None);

        var verifierA = new Mock<IVerifier>();
        verifierA.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.VerificationFailed);
        var verifierB = new Mock<IVerifier>();
        verifierB.Setup(v => v.Verify(It.IsAny<Stream>(), context)).Returns(VerificationResult.Success);

        _manager.Verifiers["file"] = new List<IVerifier> { verifierA.Object, verifierB.Object };

        var result = _manager.Verify(file, context);
        Assert.Equal(VerificationResult.VerificationFailed, result);
    }
}