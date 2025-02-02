using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Validation;

public class HashDownloadValidatorTest : CommonTestBase
{
    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
    }

    [Fact]
    public void Ctor_ArgsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HashDownloadValidator(null, HashTypeKey.None, null!));
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
    public void Ctor_ValidateCorrect(HashTypeKey type, byte[] data)
    {
        _ = new HashDownloadValidator(data, type, ServiceProvider);
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
    public void Ctor_InvalidateCorrect(HashTypeKey type, byte[] data)
    {
        Assert.Throws<ArgumentException>(() => new HashDownloadValidator(data, type, ServiceProvider));
    }

    [Fact]
    public async Task Validate_NullStream_Throws()
    {
        var validator = new HashDownloadValidator(null, HashTypeKey.None, ServiceProvider);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await validator.Validate(null!, 0));
    }

    [Fact]
    public async Task Validate_NoneHashType()
    {
        var validator = new HashDownloadValidator(null, HashTypeKey.None, ServiceProvider);
        var result = await validator.Validate(new MemoryStream(new byte[3]), 0);
        Assert.True(result);
    }

    [Fact]
    public async Task Validate_StreamNotSeekable_ThrowsNotSupportedException()
    {
        var validator = new HashDownloadValidator(null, HashTypeKey.None, ServiceProvider);
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await validator.Validate(new NonSeekableStream(), 0));
    }

    [Theory]
    [MemberData(nameof(ValidContextData))]
    public async Task Validate_HashesToNotMatch(HashTypeKey hashType, byte[] notExpectedHash)
    {
        if (hashType == HashTypeKey.None)
            return;

        const long actualDownloadedBytes = 3;
        var dlStream = new MemoryStream([0, 1, 2, 3, 4]);

        // Pretend the download progressed to EOF
        dlStream.Position = dlStream.Length;

        // notExpectedHash is always empty
        var validator = new HashDownloadValidator(notExpectedHash, hashType, ServiceProvider);
        var result = await validator.Validate(dlStream, actualDownloadedBytes);
        Assert.False(result);
    }

    public static IEnumerable<object[]> Validate_Match_Data()
    {
        // The actual data to hash is [2, 3, 4]
        yield return [HashTypeKey.None, ""]; // Hash is not used.
        yield return [HashTypeKey.MD5, "13427305830A139207A3DA251A52B53C"];
        yield return [HashTypeKey.SHA1, "6F74E8562462DC98C09E9934311F327788817395"];
        yield return [HashTypeKey.SHA256, "1F528FFD2895634C176537C055DAA5C0971B7915519999337A0E355410D8FD98"];
        yield return [HashTypeKey.SHA384, "845350D2E58AF7445DFD9224FAC0B764EE04FCC77BF2B02F0C1B00634AB2E7F16EAC0D89E031E977677FFFF13EB25882"];
        yield return [HashTypeKey.SHA512, "31C5EA6CB50F6DF3E1110E08BC3FBE3F5E02EE959E2AA6D2106C6B30429DD0B6E183DB5AA35973B1998A534956C78A8B117239FD39F63F1256D867F11CB9A073"];
    }

    [Theory]
    [MemberData(nameof(Validate_Match_Data))]
    public async Task Validate_HashesMatch(HashTypeKey hashType, string expectedHashString)
    { 
        const long actualDownloadedBytes = 3;
        var dlStream = new MemoryStream([0, 1, 2, 3, 4]);

        // Pretend the download progressed to EOF
        dlStream.Position = dlStream.Length;

        var expectedHash = ConvertHexStringToByteArray(expectedHashString);

        var validator = new HashDownloadValidator(expectedHash, hashType, ServiceProvider);
        var result = await validator.Validate(dlStream, actualDownloadedBytes);
        Assert.True(result);

        if (hashType != HashTypeKey.None) 
            Assert.Equal(5, dlStream.Position);
    }


    public static byte[] ConvertHexStringToByteArray(string hexString)
    {
#if NET
        return Convert.FromHexString(hexString);
#else
        var data = new byte[hexString.Length / 2];
        for (var index = 0; index < data.Length; index++)
        {
            var byteValue = hexString.Substring(index * 2, 2);
            data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return data;
#endif
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