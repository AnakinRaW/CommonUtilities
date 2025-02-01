using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;
using Xunit;
using System.Collections.Generic;
using System.Globalization;
#if NET8_0_OR_GREATER
using System.Security.Cryptography;
#endif

namespace AnakinRaW.CommonUtilities.Test.Hashing;

public class HashingServiceTest
{
    private readonly HashingService _hashingService;
    private readonly MockFileSystem _fileSystem = new();

    public HashingServiceTest()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(_fileSystem);
        sc.AddSingleton<IHashAlgorithmProvider>(_ => new AlwaysOneHashProvider());
        sc.AddSingleton<IHashAlgorithmProvider>(_ => new WrongOutputSizeProvider());
        _hashingService = new HashingService(sc.BuildServiceProvider());
    }

    [Fact]
    public void Ctor_ProviderAlreadyExists_Throws()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(_fileSystem);
        sc.AddSingleton<IHashAlgorithmProvider>(_ => new AlwaysOneHashProvider());
        sc.AddSingleton<IHashAlgorithmProvider>(_ => new AlwaysOneHashProvider());
        Assert.Throws<InvalidOperationException>(() => new HashingService(sc.BuildServiceProvider()));
    }

    [Fact]
    public void GetHash_FromFile_ThrowsFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), HashTypeKey.SHA512));
        Assert.Throws<FileNotFoundException>(() =>
            _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), new Span<byte>(new byte[1]),
                HashTypeKey.SHA512));
    }

    [Fact]
    public void GetHash_ProviderNotFound_ThrowsHashProviderNotFoundException()
    {
        _fileSystem.Initialize().WithFile("test.txt");

        var notExistingProvider = new HashTypeKey("SomeProvider", 1);
        var someSource = Array.Empty<byte>();
        var someDestination = new byte[1];
        var someStream = new MemoryStream(someSource);

        Assert.Throws<HashProviderNotFoundException>(() => _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), notExistingProvider));
        Assert.Throws<HashProviderNotFoundException>(() => _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), new Span<byte>(someDestination), notExistingProvider));
        Assert.Throws<HashProviderNotFoundException>(() => _hashingService.GetHash(new ReadOnlySpan<byte>(someSource), someDestination.AsSpan(), notExistingProvider));
        Assert.Throws<HashProviderNotFoundException>(() => _hashingService.GetHash(someStream, someDestination.AsSpan(), notExistingProvider));
        Assert.Throws<HashProviderNotFoundException>(() => _hashingService.GetHash(someStream, notExistingProvider));
        Assert.Throws<HashProviderNotFoundException>(() => _hashingService.GetHash("", Encoding.ASCII, notExistingProvider));
        Assert.Throws<HashProviderNotFoundException>(() => _hashingService.GetHash("", Encoding.ASCII, new Span<byte>(someDestination), notExistingProvider));
        Assert.Throws<HashProviderNotFoundException>(() => _hashingService.GetHash("".AsSpan(), new Span<byte>(someDestination), Encoding.ASCII, notExistingProvider));
        Assert.Throws<HashProviderNotFoundException>(() => _hashingService.GetHash(someSource, notExistingProvider));
    }

    [Fact]
    public async Task GetHashAsync_ProviderNotFound_ThrowsHashProviderNotFoundException()
    {
        _fileSystem.Initialize().WithFile("test.txt");

        var notExistingProvider = new HashTypeKey("SomeProvider", 1);
        var someSource = Array.Empty<byte>();
        var someDestination = new byte[1];
        var someStream = new MemoryStream(someSource);

        await Assert.ThrowsAsync<HashProviderNotFoundException>(async () => await _hashingService.GetHashAsync(_fileSystem.FileInfo.New("test.txt"), notExistingProvider));
        await Assert.ThrowsAsync<HashProviderNotFoundException>(async () => await _hashingService.GetHashAsync(_fileSystem.FileInfo.New("test.txt"), someDestination.AsMemory(), notExistingProvider));
        await Assert.ThrowsAsync<HashProviderNotFoundException>(async () => await _hashingService.GetHashAsync(someStream, notExistingProvider));
        await Assert.ThrowsAsync<HashProviderNotFoundException>(async () => await _hashingService.GetHashAsync(someStream, someDestination.AsMemory(), notExistingProvider));
    }


    [Fact]
    public void GetHash_AlwaysOneProvider_DestinationTooShort_ThrowsIndexOutOfRangeException()
    {
        _fileSystem.Initialize().WithFile("test.txt");

        var provider = AlwaysOneHashProvider.AlwaysOneProviderKey;
        var someSource = Array.Empty<byte>();
        var someDestination = Array.Empty<byte>();
        var someStream = new MemoryStream(someSource);

        Assert.Throws<ArgumentException>(() => _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), new Span<byte>(someDestination), provider));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash(new ReadOnlySpan<byte>(someSource), someDestination.AsSpan(), provider));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash(someStream, someDestination.AsSpan(), provider));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash("", Encoding.ASCII, new Span<byte>(someDestination), provider));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash("".AsSpan(), new Span<byte>(someDestination), Encoding.ASCII, provider));
    }

    [Fact]
    public async Task GetHashAsync_AlwaysOneProvider_DestinationTooShort_ThrowsIndexOutOfRangeException()
    {
        _fileSystem.Initialize().WithFile("test.txt");

        var provider = AlwaysOneHashProvider.AlwaysOneProviderKey;
        var someSource = Array.Empty<byte>();
        var someDestination = Array.Empty<byte>();
        var someStream = new MemoryStream(someSource);

        await Assert.ThrowsAsync<ArgumentException>(async () => await _hashingService.GetHashAsync(_fileSystem.FileInfo.New("test.txt"), someDestination.AsMemory(), provider));
        await Assert.ThrowsAsync<ArgumentException>(async () => await _hashingService.GetHashAsync(someStream, someDestination.AsMemory(), provider));
    }

    [Theory]
    [MemberData(nameof(ProviderKnownHashTypes))]
    public void GetHash_NullSpanShouldNotThrow(HashTypeKey hashKey)
    {
        ReadOnlySpan<char> nullSpan = [];
        _hashingService.GetHash(nullSpan, stackalloc byte[hashKey.HashSize], Encoding.ASCII, hashKey);
    }

    [Theory]
    [MemberData(nameof(ProviderKnownHashTypes))]
    public void GetHash_DestinationTooSmall_ThrowsArgumentException(HashTypeKey hashKey)
    {
        _fileSystem.Initialize().WithFile("test.txt");

        var someSource = Array.Empty<byte>();
        var someStream = new MemoryStream(someSource);

        Assert.Throws<ArgumentException>(() => _hashingService.GetHash("someData".AsSpan(), stackalloc byte[hashKey.HashSize - 1], Encoding.ASCII, hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash("someData".AsSpan(), [], Encoding.ASCII, hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash("someData".AsSpan(), Span<byte>.Empty, Encoding.ASCII, hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash("someData", Encoding.ASCII, stackalloc byte[hashKey.HashSize - 1], hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash("someData", Encoding.ASCII, [], hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash("someData", Encoding.ASCII, Span<byte>.Empty, hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash(someStream, stackalloc byte[hashKey.HashSize - 1], hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash(someStream, [], hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash(someStream, Span<byte>.Empty, hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), stackalloc byte[hashKey.HashSize - 1], hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), [], hashKey));
        Assert.Throws<ArgumentException>(() => _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), Span<byte>.Empty, hashKey));
    }

    [Fact]
    public void GetHash_WrongOutputSizeProvider_HashWrongSize_ThrowsInvalidOperationException()
    {
        _fileSystem.Initialize().WithFile("test.txt");

        var provider = WrongOutputSizeProvider.WrongOutputSize;
        var someSource = Array.Empty<byte>();
        var someDestination = new byte[2];
        var someStream = new MemoryStream(someSource);

        Assert.Throws<InvalidOperationException>(() => _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), provider));
        Assert.Throws<InvalidOperationException>(() => _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), new Span<byte>(someDestination), provider));
        Assert.Throws<InvalidOperationException>(() => _hashingService.GetHash(new ReadOnlySpan<byte>(someSource), someDestination.AsSpan(), provider));
        Assert.Throws<InvalidOperationException>(() => _hashingService.GetHash(someStream, someDestination.AsSpan(), provider));
        Assert.Throws<InvalidOperationException>(() => _hashingService.GetHash(someStream, provider));
        Assert.Throws<InvalidOperationException>(() => _hashingService.GetHash("", Encoding.ASCII, provider));
        Assert.Throws<InvalidOperationException>(() => _hashingService.GetHash("", Encoding.ASCII, new Span<byte>(someDestination), provider));
        Assert.Throws<InvalidOperationException>(() => _hashingService.GetHash("".AsSpan(), new Span<byte>(someDestination), Encoding.ASCII, provider));
        Assert.Throws<InvalidOperationException>(() => _hashingService.GetHash(someSource, provider));
    }

    [Fact]
    public async Task GetHashAsync_WrongOutputSizeProvider_HashWrongSize_ThrowsInvalidOperationException()
    {
        _fileSystem.Initialize().WithFile("test.txt");

        var notExistingProvider = WrongOutputSizeProvider.WrongOutputSize;
        var someSource = Array.Empty<byte>();
        var someDestination = new byte[2];
        var someStream = new MemoryStream(someSource);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _hashingService.GetHashAsync(_fileSystem.FileInfo.New("test.txt"), notExistingProvider));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _hashingService.GetHashAsync(_fileSystem.FileInfo.New("test.txt"), someDestination.AsMemory(), notExistingProvider));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _hashingService.GetHashAsync(someStream, someDestination.AsMemory(), notExistingProvider));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _hashingService.GetHashAsync(someStream, notExistingProvider));
    }


    [Fact]
    public void GetHash_AlwaysOneProvider()
    {
        _fileSystem.Initialize().WithFile("test.txt");

        var provider = AlwaysOneHashProvider.AlwaysOneProviderKey;
        var someSource = Array.Empty<byte>();
        var someStream = new MemoryStream(someSource);

        var destination = new byte[] { 0, 0 };

        var expectedHashExact = new byte[] { 1 };
        var expectedHashJoint = new byte[] { 1, 0 };

        Assert.Equal(expectedHashExact, _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), provider));

        Assert.Equal(1, _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), destination.AsSpan(), provider));
        Assert.Equal(expectedHashJoint, destination);

        Assert.Equal(1, _hashingService.GetHash(new ReadOnlySpan<byte>(someSource), destination.AsSpan(), provider));
        Assert.Equal(expectedHashJoint, destination);

        Assert.Equal(1, _hashingService.GetHash(someStream, destination.AsSpan(), provider));
        Assert.Equal(expectedHashJoint, destination);

        Assert.Equal(expectedHashExact, _hashingService.GetHash(someStream, provider));

        Assert.Equal(expectedHashExact, _hashingService.GetHash("", Encoding.ASCII, provider));

        Assert.Equal(1, _hashingService.GetHash("", Encoding.ASCII, new Span<byte>(destination), provider));
        Assert.Equal(expectedHashJoint, destination);

        Assert.Equal(1, _hashingService.GetHash("".AsSpan(), new Span<byte>(destination), Encoding.ASCII, provider));
        Assert.Equal(expectedHashJoint, destination);

        Assert.Equal(expectedHashExact, _hashingService.GetHash(someSource, provider));
    }

    [Fact]
    public async Task GetHashAsync_AlwaysOneProvider()
    {
        _fileSystem.Initialize().WithFile("test.txt");

        var provider = AlwaysOneHashProvider.AlwaysOneProviderKey;
        var someSource = Array.Empty<byte>();
        var someStream = new MemoryStream(someSource);

        var destination = new byte[] { 0, 0 };

        var expectedHashExact = new byte[] { 1 };
        var expectedHashJoint = new byte[] { 1, 0 };

        Assert.Equal(expectedHashExact, await _hashingService.GetHashAsync(_fileSystem.FileInfo.New("test.txt"), provider));

        Assert.Equal(1, await _hashingService.GetHashAsync(_fileSystem.FileInfo.New("test.txt"), destination.AsMemory(), provider));
        Assert.Equal(expectedHashJoint, destination);

        Assert.Equal(expectedHashExact, await _hashingService.GetHashAsync(someStream, provider));

        Assert.Equal(1, await _hashingService.GetHashAsync(someStream, destination.AsMemory(), provider));
        Assert.Equal(expectedHashJoint, destination);
    }


    [Theory]
    [MemberData(nameof(ProviderHashTestData_MD5))]
    [MemberData(nameof(ProviderHashTestData_SHA1))]
    [MemberData(nameof(ProviderHashTestData_SHA256))]
    [MemberData(nameof(ProviderHashTestData_SHA384))]
    [MemberData(nameof(ProviderHashTestData_SHA512))]
#if NET8_0_OR_GREATER
    [MemberData(nameof(ProviderHashTestData_SHA3_256))]
    [MemberData(nameof(ProviderHashTestData_SHA3_384))]
    [MemberData(nameof(ProviderHashTestData_SHA3_512))]
#endif

    public void GetHash_DefaultProviders(HashTypeKey hashType, string input, string expectedHashString)
    {
        _fileSystem.Initialize().WithFile("test.txt").Which(a => a.HasStringContent(input));

        var expectedHash = HexToByteArray(expectedHashString);
        var expectedSize = hashType.HashSize;

        var someSource = AsciiBytes(input);
        var someStream = new MemoryStream(someSource);
        var destination = new byte[expectedSize];


        Assert.Equal(expectedHash, _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), hashType));

        Assert.Equal(expectedSize, _hashingService.GetHash(_fileSystem.FileInfo.New("test.txt"), destination.AsSpan(), hashType));
        Assert.Equal(expectedHash, destination);

        Assert.Equal(expectedSize, _hashingService.GetHash(new ReadOnlySpan<byte>(someSource), destination.AsSpan(), hashType));
        Assert.Equal(expectedHash, destination);

        Assert.Equal(expectedSize, _hashingService.GetHash(someStream, destination.AsSpan(), hashType));
        Assert.Equal(expectedHash, destination);

        someStream.Seek(0, SeekOrigin.Begin);

        Assert.Equal(expectedHash, _hashingService.GetHash(someStream, hashType));

        Assert.Equal(expectedHash, _hashingService.GetHash(input, Encoding.ASCII, hashType));

        Assert.Equal(expectedSize, _hashingService.GetHash(input, Encoding.ASCII, new Span<byte>(destination), hashType));
        Assert.Equal(expectedHash, destination);

        Assert.Equal(expectedSize, _hashingService.GetHash(input.AsSpan(), new Span<byte>(destination), Encoding.ASCII, hashType));
        Assert.Equal(expectedHash, destination);

        Assert.Equal(expectedHash, _hashingService.GetHash(someSource, hashType));
    }

    [Theory]
    [MemberData(nameof(ProviderHashTestData_MD5))]
    [MemberData(nameof(ProviderHashTestData_SHA1))]
    [MemberData(nameof(ProviderHashTestData_SHA256))]
    [MemberData(nameof(ProviderHashTestData_SHA384))]
    [MemberData(nameof(ProviderHashTestData_SHA512))]
#if NET8_0_OR_GREATER
    [MemberData(nameof(ProviderHashTestData_SHA3_256))]
    [MemberData(nameof(ProviderHashTestData_SHA3_384))]
    [MemberData(nameof(ProviderHashTestData_SHA3_512))]
#endif
    public async Task GetHashAsync_DefaultProviders(HashTypeKey hashType, string input, string expectedHashString)
    {
        _fileSystem.Initialize().WithFile("test.txt").Which(a => a.HasStringContent(input));

        var expectedHash = HexToByteArray(expectedHashString);
        var expectedSize = hashType.HashSize;

        var someSource = AsciiBytes(input);
        var someStream = new MemoryStream(someSource);
        var destination = new byte[expectedSize];

        Assert.Equal(expectedHash, await _hashingService.GetHashAsync(_fileSystem.FileInfo.New("test.txt"), hashType));

        Assert.Equal(expectedSize, await _hashingService.GetHashAsync(_fileSystem.FileInfo.New("test.txt"), destination.AsMemory(), hashType));
        Assert.Equal(expectedHash, destination);

        Assert.Equal(expectedHash, await _hashingService.GetHashAsync(someStream, hashType));

        someStream.Seek(0, SeekOrigin.Begin);

        Assert.Equal(expectedSize, await _hashingService.GetHashAsync(someStream, destination.AsMemory(), hashType));
        Assert.Equal(expectedHash, destination);
    }


    public static IEnumerable<object[]> ProviderHashTestData_MD5()
    {
        yield return [HashTypeKey.MD5, "", "d41d8cd98f00b204e9800998ecf8427e"];
        yield return [HashTypeKey.MD5, "a", "0cc175b9c0f1b6a831c399e269772661"];
        yield return [HashTypeKey.MD5, "abc", "900150983cd24fb0d6963f7d28e17f72"];
        yield return [HashTypeKey.MD5, "message digest", "f96b697d7cb7938d525a2f31aaf161d0"];
        yield return [HashTypeKey.MD5, RepeatString("1234567890", 8), "57edf4a22be3c955ac49da2e2107b67a"];
        yield return [HashTypeKey.MD5, RepeatString("0102030405060708", 1024), "5fc6366852074da6e4795a014574282c"];
    }

    public static IEnumerable<object[]> ProviderHashTestData_SHA1()
    {
        yield return [HashTypeKey.SHA1, "", "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709"];
        yield return [HashTypeKey.SHA1, "abc", "A9993E364706816ABA3E25717850C26C9CD0D89D"];
        yield return [HashTypeKey.SHA1, "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "84983E441C3BD26EBAAE4AA1F95129E5E54670F1"];
        yield return [HashTypeKey.SHA1, RepeatString("0102030405060708", 1024), "fc8053215c935a5e9cdc51b94bb40b3e66128d41"];
    }

    public static IEnumerable<object[]> ProviderHashTestData_SHA256()
    {
        yield return [HashTypeKey.SHA256, "", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"];
        yield return [HashTypeKey.SHA256, "abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"];
        yield return [HashTypeKey.SHA256, "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1"];
        yield return [HashTypeKey.SHA256, RepeatString("0102030405060708", 1024), "cedca4ad2cce0d0b399931708684800cd16be396ffa5af51297a091650aa3610"];
    }

    public static IEnumerable<object[]> ProviderHashTestData_SHA384()
    {
        yield return [HashTypeKey.SHA384, "", "38B060A751AC96384CD9327EB1B1E36A21FDB71114BE07434C0CC7BF63F6E1DA274EDEBFE76F65FBD51AD2F14898B95B"];
        yield return [HashTypeKey.SHA384, "abc", "CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED8086072BA1E7CC2358BAECA134C825A7"];
        yield return [HashTypeKey.SHA384, "abcdefghbcdefghicdefghijdefghijkefghijklfghijklmghijklmnhijklmnoijklmnopjklmnopqklmnopqrlmnopqrsmnopqrstnopqrstu", "09330C33F71147E83D192FC782CD1B4753111B173B3B05D22FA08086E3B0F712FCC7C71A557E2DB966C3E9FA91746039"];
        yield return [HashTypeKey.SHA384, RepeatString("0102030405060708", 1024), "d9deec18b8ec0d31270eaeaaf3bcb1de55f1d81482a55d2c023bad873175f1694d8c28e8138d9147dc180e679cd79c58"];
    }

    public static IEnumerable<object[]> ProviderHashTestData_SHA512()
    {
        yield return [HashTypeKey.SHA512, "", "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e"];
        yield return [HashTypeKey.SHA512, "abc", "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f"];
        yield return [HashTypeKey.SHA512, "abcdefghbcdefghicdefghijdefghijkefghijklfghijklmghijklmnhijklmnoijklmnopjklmnopqklmnopqrlmnopqrsmnopqrstnopqrstu", "8e959b75dae313da8cf4f72814fc143f8f7779c6eb9f7fa17299aeadb6889018501d289e4900f7e4331b99dec4b5433ac7d329eeb6dd26545e96e55b874be909"];
        yield return [HashTypeKey.SHA512, RepeatString("0102030405060708", 1024), "da1bdf4632ea5ee0724a57a9bc6fd409d7f8f7417373356281ce36f82b510da95c7dff7d64a43b3cf4854894e124f56b349749a3f76b41611c01fee739f4d923"];
    }

    public static IEnumerable<object[]> ProviderKnownHashTypes()
    {
        yield return [HashTypeKey.MD5];
        yield return [HashTypeKey.SHA1];
        yield return [HashTypeKey.SHA256];
        yield return [HashTypeKey.SHA384];
        yield return [HashTypeKey.SHA512];
#if NET8_0_OR_GREATER
        if (SHA3_256.IsSupported)
            yield return [HashTypeKey.SHA3_256];
        if (SHA3_384.IsSupported)
            yield return [HashTypeKey.SHA3_384];
        if (SHA3_512.IsSupported)
            yield return [HashTypeKey.SHA3_512];
#endif
    }

#if NET8_0_OR_GREATER

    public static IEnumerable<object[]> ProviderHashTestData_SHA3_256()
    {
        if (!SHA3_256.IsSupported)
            yield break;

        yield return [HashTypeKey.SHA3_256, "", "a7ffc6f8bf1ed76651c14756a061d662f580ff4de43b49fa82d80a4b80f8434a"];
        yield return [HashTypeKey.SHA3_256, RepeatString("0102030405060708", 1024), "5e80dd4330d9124adce40a043f166d7e0f6853050fd99919c7b1436ee0a538e9"];
        yield return [HashTypeKey.SHA3_256, RepeatString("0102030405060708", 1025), "5dbbd15ba5745412a79835cc4bec1bede925da06eca7a5bbf50c38a6ec1c49bc"];
    }

    public static IEnumerable<object[]> ProviderHashTestData_SHA3_384()
    {
        if (!SHA3_384.IsSupported)
            yield break;
        yield return [HashTypeKey.SHA3_384, "", "0c63a75b845e4f7d01107d852e4c2485c51a50aaaa94fc61995e71bbee983a2ac3713831264adb47fb6bd1e058d5f004"];
        yield return [HashTypeKey.SHA3_384, RepeatString("0102030405060708", 1024), "0aa96c328926f2faa796dc75a104e200f5b497beb0313e8822b471efebbb39cef02687e33787883a87c18f35856dcad1"];
        yield return [HashTypeKey.SHA3_384, RepeatString("0102030405060708", 1025), "e2d1714c8011e7b90550123006128d90fa464cb71903e4aa67342bc8780eb43ae099a2d8610e0ba3061f4f3792d344a7"];
    }

    public static IEnumerable<object[]> ProviderHashTestData_SHA3_512()
    {
        if (!SHA3_512.IsSupported)
            yield break;
        yield return [HashTypeKey.SHA3_512, "", "a69f73cca23a9ac5c8b567dc185a756e97c982164fe25859e0d1dcc1475c80a615b2123af1f5f94c11e3e9402c3ac558f500199d95b6d3e301758586281dcd26"];
        yield return [HashTypeKey.SHA3_512, RepeatString("0102030405060708", 1024), "b5ec7fe7061c944b65f42a3193ebafcc3b35f063dc2ac7a5af05140b2439c425e4d9e63bc97103f704a7b6849a1986cec743ac288ca2f123e82c0ce60b714615"];
        yield return [HashTypeKey.SHA3_512, RepeatString("0102030405060708", 1025), "ea418b3d279a9b25ddc6f8a294006c63068cbd4b872163365f7d11f6f287c8291adc0e3b77999db9606a40c989d7eca405247162104feec1d5a46e59404692a2"];
    }
    
#endif


    internal class AlwaysOneHashProvider : IHashAlgorithmProvider
    {
        public static HashTypeKey AlwaysOneProviderKey = new("AlwaysOne", 1);

        public HashTypeKey SupportedHashType { get; } = AlwaysOneProviderKey;

        public int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            destination[0] = 1;
            return 1;
        }

        public int HashData(Stream source, Span<byte> destination)
        {
            destination[0] = 1;
            return 1;
        }

        public ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellation = default)
        {
            destination.Span[0] = 1;
            return new ValueTask<int>(1);
        }
    }


    // Hash Size is 2 but this provider only fills one.
    internal class WrongOutputSizeProvider : IHashAlgorithmProvider
    {
        public static HashTypeKey WrongOutputSize = new("WrongOutputSize", 2);

        public HashTypeKey SupportedHashType { get; } = WrongOutputSize;

        public int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            destination[0] = 1;
            return 1;
        }

        public int HashData(Stream source, Span<byte> destination)
        {
            destination[0] = 1;
            return 1;
        }

        public ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellation = default)
        {
            destination.Span[0] = 1;
            return new ValueTask<int>(1);
        }
    }

    internal static byte[] HexToByteArray(string hexString)
    {
        var bytes = new byte[hexString.Length / 2];
        for (var i = 0; i < hexString.Length; i += 2)
        {
            var s = hexString.Substring(i, 2);
            bytes[i / 2] = byte.Parse(s, NumberStyles.HexNumber, null);
        }
        return bytes;
    }

    internal static byte[] AsciiBytes(string s)
    {
        var bytes = new byte[s.Length];
        for (var i = 0; i < s.Length; i++) 
            bytes[i] = (byte)s[i];
        return bytes;
    }

    internal static string RepeatString(string valueToRepeat, int iteration)
    {
        var stringBuilder = new StringBuilder();
        for (var i = 0; i < iteration; i++) 
            stringBuilder.Append(valueToRepeat);
        return stringBuilder.ToString();
    }
}