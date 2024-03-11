using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.Hashing.Providers;

internal class SHA1HashProvider : HashAlgorithmProviderBase
{
    public override HashTypeKey SupportedHashType => HashTypeKey.SHA1;

    protected override int HashDataNetCore(ReadOnlySpan<byte> source, Span<byte> destination)
    {
#if NET
        return SHA1.HashData(source, destination);
#else
        throw new NotSupportedException();
#endif
    }

    protected override int HashDataNetCore(Stream source, Span<byte> destination)
    {
#if NET
        return SHA1.HashData(source, destination);
#else
        throw new NotSupportedException();
#endif
    }

    protected override ValueTask<int> HashDataAsyncNetCore(Stream source, Memory<byte> destination, CancellationToken cancellation = default)
    {
#if NET
        return SHA1.HashDataAsync(source, destination, cancellation);
#else
        throw new NotSupportedException();
#endif
    }

    protected override HashAlgorithm CreateHashAlgorithm()
    {
        return SHA1.Create();
    }
}