using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.Hashing.Providers;

internal class SHA256HashProvider : HashAlgorithmProviderBase
{
    public override HashTypeKey SupportedHashType => HashTypeKey.SHA256;

    protected override int HashDataNetCore(ReadOnlySpan<byte> source, Span<byte> destination)
    {
#if NET
        return SHA256.HashData(source, destination);
#else
        throw new NotSupportedException();
#endif
    }

    protected override int HashDataNetCore(Stream source, Span<byte> destination)
    {
#if NET
        return SHA256.HashData(source, destination);
#else
        throw new NotSupportedException();
#endif
    }

    protected override ValueTask<int> HashDataAsyncNetCore(Stream source, Memory<byte> destination, CancellationToken cancellation = default)
    {
#if NET
        return SHA256.HashDataAsync(source, destination, cancellation);
#else
        throw new NotSupportedException();
#endif
    }

    protected override HashAlgorithm CreateHashAlgorithm()
    {
        return SHA256.Create();
    }
}