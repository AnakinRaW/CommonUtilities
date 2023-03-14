using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AnakinRaW.CommonUtilities.DownloadManager.Verification;
using AnakinRaW.CommonUtilities.DownloadManager.Verification.HashVerification;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Verification;

public class HashVerifierTest
{
    private readonly HashVerifier _verifier;
    private readonly Mock<IHashingService> _hashing;
    private readonly MockFileSystem _fileSystem;
    public HashVerifierTest()
    {
        _fileSystem = new MockFileSystem();
        _hashing = new Mock<IHashingService>();
        var sc = new ServiceCollection();
        sc.AddTransient(_ => _hashing.Object);
        sc.AddSingleton<IFileSystem>(_fileSystem);
        _verifier = new HashVerifier(sc.BuildServiceProvider());
    }

    [Fact]
    public void TestStreamIsNotFileStream()
    {
        Assert.Throws<ArgumentException>(() =>
            _verifier.Verify(new MemoryStream(), new HashVerificationContext(default)));
    }

    [Fact]
    public void TestFileNotFound()
    {
        var path = _fileSystem.FileInfo.New("test.txt").FullName;
        var stream = new MemoryStream();
        Assert.Throws<FileNotFoundException>(() =>
            _verifier.Verify(stream, path, new HashVerificationContext(default)));
    }

    [Fact]
    public void TestInvalidVerificationContext()
    {
        _fileSystem.AddFile("test.txt", new MockFileData(string.Empty));
        var path = _fileSystem.FileInfo.New("test.txt").FullName;
        var stream = new MemoryStream();
        var result = _verifier.Verify(stream, path, new HashVerificationContext(new HashingData(HashType.MD5, Array.Empty<byte>())));
        Assert.Equal(VerificationResult.VerificationContextError, result);
    }
    
    [Fact]
    public void TestHashTypeNoneAlwaysSucceeds()
    {
        _fileSystem.AddFile("test.txt", new MockFileData(string.Empty));
        var path = _fileSystem.FileInfo.New("test.txt").FullName;
        var stream = new MemoryStream();

        _hashing.Setup(h => h.GetStreamHash(stream, It.IsAny<HashType>(), It.IsAny<bool>()))
            .Returns(new byte[] { 1 });

        var result = _verifier.Verify(stream, path, HashVerificationContext.None);
        Assert.Equal(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerificationFailed()
    {
        _fileSystem.AddFile("test.txt", new MockFileData(string.Empty));
        var path = _fileSystem.FileInfo.New("test.txt").FullName;
        var stream = new MemoryStream();

        _hashing.Setup(h => h.GetStreamHash(stream, It.IsAny<HashType>(), It.IsAny<bool>()))
            .Returns(new byte[]{1});

        var result = _verifier.Verify(stream, path, new HashVerificationContext(new byte[16], HashType.MD5));
        Assert.Equal(VerificationResult.VerificationFailed, result);
    }

    [Fact]
    public void TestVerificationSucceedsFileStream()
    {
        _fileSystem.AddFile("test.txt", new MockFileData(string.Empty));
        var stream = _fileSystem.FileInfo.New("test.txt").OpenRead();

        var hash = new byte[16];

        _hashing.Setup(h => h.GetStreamHash(stream, It.IsAny<HashType>(), It.IsAny<bool>()))
            .Returns(hash);

        var result = _verifier.Verify(stream, new HashVerificationContext(hash, HashType.MD5));
        Assert.Equal(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerificationSucceeds()
    {
        _fileSystem.AddFile("test.txt", new MockFileData(string.Empty));
        var path = _fileSystem.FileInfo.New("test.txt").FullName;
        var stream = new MemoryStream();

        var hash = new byte[16];

        _hashing.Setup(h => h.GetStreamHash(stream, It.IsAny<HashType>(), It.IsAny<bool>()))
            .Returns(hash);

        var result = _verifier.Verify(stream, path, new HashVerificationContext(hash, HashType.MD5));
        Assert.Equal(VerificationResult.Success, result);
    }

    [Fact]
    public void TestVerificationWithException()
    {
        _fileSystem.AddFile("test.txt", new MockFileData(string.Empty));
        var path = _fileSystem.FileInfo.New("test.txt").FullName;
        var stream = new MemoryStream();

        var hash = new byte[16];

        _hashing.Setup(h => h.GetStreamHash(stream, It.IsAny<HashType>(), It.IsAny<bool>()))
            .Throws<Exception>();

        var result = _verifier.Verify(stream, path, new HashVerificationContext(hash, HashType.MD5));
        Assert.Equal(VerificationResult.Exception, result);
    }
}