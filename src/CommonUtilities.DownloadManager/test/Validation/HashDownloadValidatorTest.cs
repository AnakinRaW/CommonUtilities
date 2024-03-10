using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
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
        yield return [HashTypeKey.None, null];
        yield return [HashTypeKey.None, Array.Empty<byte>()];
        yield return [HashTypeKey.MD5, new byte[16]];
        yield return [HashTypeKey.SHA1, new byte[20]];
        yield return [HashTypeKey.SHA256, new byte[32]];
        yield return [HashTypeKey.SHA384, new byte[48]];
        yield return [HashTypeKey.SHA512, new byte[64]];
    }

    [Theory]
    [MemberData(nameof(ValidContextData))]
    public void Test_Ctor_ValidateCorrect(HashTypeKey type, byte[] data)
    {
        _ = new HashDownloadValidator(data, type, _serviceProvider);
    }

    public static IEnumerable<object[]> InvalidContextData()
    {
        yield return [HashTypeKey.None, new byte[1]];
        yield return [HashTypeKey.MD5, new byte[1]];
        yield return [HashTypeKey.SHA1, new byte[1]];
        yield return [HashTypeKey.SHA256, new byte[1]];
        yield return [HashTypeKey.SHA384, new byte[1]];
        yield return [HashTypeKey.SHA512, new byte[1]];
        yield return [HashTypeKey.MD5, Array.Empty<byte>()];
        yield return [HashTypeKey.SHA1, Array.Empty<byte>()];
        yield return [HashTypeKey.SHA256, Array.Empty<byte>()];
        yield return [HashTypeKey.SHA384, Array.Empty<byte>()];
        yield return [HashTypeKey.SHA512, Array.Empty<byte>()];
        yield return [HashTypeKey.MD5, null];
        yield return [HashTypeKey.SHA1, null];
        yield return [HashTypeKey.SHA256, null];
        yield return [HashTypeKey.SHA384, null];
        yield return [HashTypeKey.SHA512, null];
    }

    [Theory]
    [MemberData(nameof(InvalidContextData))]
    public void Test_Ctor_InvalidateCorrect(HashTypeKey type, byte[] data)
    {
        Assert.Throws<ArgumentException>(() => new HashDownloadValidator(data, type, _serviceProvider));
    }
    
    [Fact]
    public async void Test_Validate_NoneHashType()
    {
        var validator = new HashDownloadValidator(null, HashTypeKey.None, _serviceProvider);
        var result = await validator.Validate(new MemoryStream(new byte[3]), 0);
        Assert.True(result);
    }

    [Fact]
    public async void Test_Validate_StreamNotSeekable_ThrowsNotSupportedException()
    {
        var validator = new HashDownloadValidator(null, HashTypeKey.None, _serviceProvider);
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await validator.Validate(new NonSeekableStream(), 0));
    }

    [Fact]
    public async void Test_Validate_HashesToNotMatch()
    {
        var expected = GenerateRandomHash(HashTypeKey.MD5);
        byte[] actual;

        do
        {
            actual = GenerateRandomHash(HashTypeKey.MD5);
        } while (actual.SequenceEqual(expected));

        _hashingService.Setup(h => h.GetHash(It.IsAny<Stream>(), HashTypeKey.MD5))
            .Returns(actual);

        var validator = new HashDownloadValidator(expected, HashTypeKey.MD5, _serviceProvider);
        var result = await validator.Validate(new MemoryStream(new byte[3]), 0);
        Assert.False(result);
    }

    [Fact]
    public async void Test_Validate_HashesMatch()
    {
        var hash = GenerateRandomHash(HashTypeKey.MD5);

        const long actualDownloadedBytes = 3;
        var dlStream = new MemoryStream([0, 1, 2, 3, 4]);

        // Pretend the download progressed to EOF
        dlStream.Position = dlStream.Length;

        _hashingService.Setup(h => h.GetHashAsync(dlStream, HashTypeKey.MD5, CancellationToken.None))
            .Callback((Stream s, HashTypeKey h, CancellationToken c) =>
            {
                Assert.Equal(dlStream.Length - actualDownloadedBytes, dlStream.Position);
            })
            .Returns(new ValueTask<byte[]>(hash));


        var validator = new HashDownloadValidator(hash, HashTypeKey.MD5, _serviceProvider);
        var result = await validator.Validate(dlStream, actualDownloadedBytes);
        Assert.True(result);

        _hashingService.Verify(v => v.GetHashAsync(dlStream, HashTypeKey.MD5, CancellationToken.None), Times.Once);
    }

    private static byte[] GenerateRandomHash(HashTypeKey hashType)
    {
        var hashSize = hashType.HashSize;
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