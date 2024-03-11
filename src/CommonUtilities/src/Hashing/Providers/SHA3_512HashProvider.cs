﻿#if NET8_0_OR_GREATER
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Hashing.Providers;

namespace AnakinRaW.CommonUtilities.Hashing.Providers;

internal class SHA3_512HashProvider : HashAlgorithmProviderBase
{
    public override HashTypeKey SupportedHashType => HashTypeKey.SHA3_512;

    protected override int HashDataNetCore(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return SHA3_512.HashData(source, destination);
    }

    protected override int HashDataNetCore(Stream source, Span<byte> destination)
    {
        return SHA3_512.HashData(source, destination);
    }

    protected override ValueTask<int> HashDataAsyncNetCore(Stream source, Memory<byte> destination,
        CancellationToken cancellation = default)
    {
        return SHA3_512.HashDataAsync(source, destination, cancellation);
    }

    protected override HashAlgorithm CreateHashAlgorithm()
    {
        return SHA3_512.Create();
    }
}
#endif