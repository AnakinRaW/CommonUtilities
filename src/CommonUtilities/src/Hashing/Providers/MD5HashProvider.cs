using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.Hashing.Providers;

internal class MD5HashProvider : HashAlgorithmProviderBase
{
    public override HashTypeKey SupportedHashType => HashTypeKey.MD5;

    protected override int HashDataNetCore(ReadOnlySpan<byte> source, Span<byte> destination)
    {
#if NET
        return MD5.HashData(source, destination);
#else
        throw new NotSupportedException();
#endif
    }

    protected override int HashDataNetCore(Stream source, Span<byte> destination)
    {
#if NET
        return MD5.HashData(source, destination);
#else
        throw new NotSupportedException();
#endif
    }

    protected override ValueTask<int> HashDataAsyncNetCore(Stream source, Memory<byte> destination, CancellationToken cancellation = default)
    {
#if NET
        return MD5.HashDataAsync(source, destination, cancellation);
#else
        throw new NotSupportedException();
#endif
    }

    protected override HashAlgorithm CreateHashAlgorithm()
    {
        return MD5.Create();
    }
}