using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using AnakinRaW.CommonUtilities.Hashing;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class HashingServiceTest
{
    private readonly HashingService _hashingService;

    public HashingServiceTest()
    {
        _hashingService = new HashingService();
    }

    [Fact]
    public void TestUnknownAlgorithm()
    {
        Assert.Throws<NotSupportedException>(() =>
            _hashingService.GetStreamHash(new MemoryStream(), HashType.None));
    }

    [Fact]
    public void TestStreamClosed()
    {
        var ms = new MemoryStream();
        _hashingService.GetStreamHash(ms, HashType.MD5);
        Assert.Throws<ObjectDisposedException>(() => ms.Position = 0);
    }

    [Fact]
    public void TestStreamKeepOpen()
    {
        var ms = new MemoryStream();
        _hashingService.GetStreamHash(ms, HashType.MD5, true);
        ms.Position = 0;
    }

    [Theory]
    [InlineData(HashType.MD5, 16)]
    [InlineData(HashType.Sha1, 20)]
    [InlineData(HashType.Sha256, 32)]
    [InlineData(HashType.Sha384, 48)]
    [InlineData(HashType.Sha512, 64)]
    public void TestCorrectAlgorithm(HashType type, byte digestSize)
    {
        var ms = new MemoryStream();
        var hash = _hashingService.GetStreamHash(ms, type);
        Assert.Equal<int>(digestSize, hash.Length);
    }

    [Fact]
    public void TestFile()
    {
        var fs = new MockFileSystem();
        fs.AddFile("file.exe", new MockFileData(string.Empty));
        var hash = _hashingService.GetFileHash(fs.FileInfo.New("file.exe"), HashType.MD5);
        var hs = ByteArrayToString(hash);
        Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", hs, StringComparer.InvariantCultureIgnoreCase);

        static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }
    }

#if NET
    [Fact]
    public async void TestFileAsync()
    {
        var fs = new MockFileSystem();
        fs.AddFile("file.exe", new MockFileData(string.Empty));
        var hash = await _hashingService.HashFileAsync(fs.FileInfo.New("file.exe"), HashType.MD5);
        var hs = ByteArrayToString(hash);
        Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", hs, StringComparer.InvariantCultureIgnoreCase);

        static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }
    }
#endif
}