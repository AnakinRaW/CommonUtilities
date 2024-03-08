using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Validation;

public class HashDownloadValidatorTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IHashingService> _hashingService = new();

    public HashDownloadValidatorTest()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(_hashingService.Object);
        _serviceProvider = sc.BuildServiceProvider();
    }

    public static IEnumerable<object[]> ValidContextData()
    {
        yield return [HashType.None, null];
        yield return [HashType.None, Array.Empty<byte>()];
        yield return [HashType.MD5, new byte[16]];
        yield return [HashType.Sha1, new byte[20]];
        yield return [HashType.Sha256, new byte[32]];
        yield return [HashType.Sha384, new byte[48]];
        yield return [HashType.Sha512, new byte[64]];
    }

    [Theory]
    [MemberData(nameof(ValidContextData))]
    public void TestValidateCorrect(HashType type, byte[] data)
    {
        _ = new HashDownloadValidator(data, type, _serviceProvider);
    }

    public static IEnumerable<object[]> InvalidContextData()
    {
        yield return [HashType.None, new byte[1]];
        yield return [HashType.MD5, new byte[1]];
        yield return [HashType.Sha1, new byte[1]];
        yield return [HashType.Sha256, new byte[1]];
        yield return [HashType.Sha384, new byte[1]];
        yield return [HashType.Sha512, new byte[1]];
        yield return [HashType.MD5, Array.Empty<byte>()];
        yield return [HashType.Sha1, Array.Empty<byte>()];
        yield return [HashType.Sha256, Array.Empty<byte>()];
        yield return [HashType.Sha384, Array.Empty<byte>()];
        yield return [HashType.Sha512, Array.Empty<byte>()];
        yield return [HashType.MD5, null];
        yield return [HashType.Sha1, null];
        yield return [HashType.Sha256, null];
        yield return [HashType.Sha384, null];
        yield return [HashType.Sha512, null];
    }

    [Theory]
    [MemberData(nameof(InvalidContextData))]
    public void Test_Ctor_InvalidateCorrect(HashType type, byte[] data)
    {
        Assert.Throws<ArgumentException>(() => new HashDownloadValidator(data, type, _serviceProvider));
    }
    
    [Fact]
    public async void Test_Validate_NoneHashType()
    {
        var validator = new HashDownloadValidator(null, HashType.None, _serviceProvider);
        var result = await validator.Validate(new MemoryStream(new byte[3]), 0);
        Assert.True(result);
    }

    [Fact]
    public async void Test_Validate_StreamNotSeekable_ThrowsNotSupportedException()
    {
        var validator = new HashDownloadValidator(null, HashType.None, _serviceProvider);
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await validator.Validate(new NonSeekableStream(), 0));
    }

    [Fact]
    public async void Test_Validate_HashesToNotMatch()
    {
        var expected = GenerateRandomHash(HashType.MD5);
        byte[] actual;

        do
        {
            actual = GenerateRandomHash(HashType.MD5);
        } while (actual.SequenceEqual(expected));

        _hashingService.Setup(h => h.GetStreamHash(It.IsAny<Stream>(), HashType.MD5))
            .Returns(actual);

        var validator = new HashDownloadValidator(expected, HashType.MD5, _serviceProvider);
        var result = await validator.Validate(new MemoryStream(new byte[3]), 0);
        Assert.False(result);
    }

    [Fact]
    public async void Test_Validate_HashesMatch()
    {
        var hash = GenerateRandomHash(HashType.MD5);

        _hashingService.Setup(h => h.GetStreamHash(It.IsAny<Stream>(), HashType.MD5))
            .Returns(hash);

        var validator = new HashDownloadValidator(hash, HashType.MD5, _serviceProvider);
        var result = await validator.Validate(new MemoryStream(new byte[3]), 0);
        Assert.True(result);
    }

    private static byte[] GenerateRandomHash(HashType hashType)
    {
        var hashSize = (int)hashType;
        var hash = new byte[hashSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(hash);
        return hash;
    }


    class NonSeekableStream : Stream
    {
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }
    }
}