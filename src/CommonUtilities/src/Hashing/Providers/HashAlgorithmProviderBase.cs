using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.Hashing.Providers;

internal abstract class HashAlgorithmProviderBase : IHashAlgorithmProvider
{
    public abstract HashTypeKey SupportedHashType { get; }

    public virtual int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
    {
#if NET5_0_OR_GREATER
        return HashDataNetCore(source, destination);
#else
        return ComputeHashWithHashAlgorithmLegacy(source, destination);
#endif
    }

    protected abstract int HashDataNetCore(ReadOnlySpan<byte> source, Span<byte> destination);


    public int HashData(Stream source, Span<byte> destination)
    {
#if NET5_0_OR_GREATER
        return HashDataNetCore(source, destination);
#else
        return ComputeHashWithHashAlgorithmLegacy(source, destination);
#endif
    }

    protected abstract int HashDataNetCore(Stream source, Span<byte> destination);


    public ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellation = default)
    {
#if NET7_0_OR_GREATER
        return HashDataAsyncNetCore(source, destination, cancellation);
#else
        return ComputeHashAsyncWithHashAlgorithmLegacy(source, destination, cancellation);
#endif
    }

    protected abstract ValueTask<int> HashDataAsyncNetCore(Stream source, Memory<byte> destination,
        CancellationToken cancellation = default);

    protected abstract HashAlgorithm CreateHashAlgorithm();

    protected int ComputeHashWithHashAlgorithmLegacy(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        using var algorithm = CreateHashAlgorithm();

#if NETSTANDARD2_1_OR_GREATER
        algorithm.TryComputeHash(source, destination, out var bytesWritten);
        return bytesWritten;
#else
        var bytes = algorithm.ComputeHash(source.ToArray());
        bytes.CopyTo(destination);
        return bytes.Length;
#endif
    }

    protected async ValueTask<int> ComputeHashAsyncWithHashAlgorithmLegacy(Stream source, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        using var algorithm = CreateHashAlgorithm();

        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0)
                algorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
            algorithm.TransformFinalBlock(buffer, 0, bytesRead);

            var hashValue = algorithm.Hash!;
            algorithm.Hash.CopyTo(destination);
            return hashValue.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }


    protected int ComputeHashWithHashAlgorithmLegacy(Stream source, Span<byte> destination)
    {
        using var algorithm = CreateHashAlgorithm();
        var hashValue = algorithm.ComputeHash(source);
        hashValue.CopyTo(destination);
        return hashValue.Length;
    }
}