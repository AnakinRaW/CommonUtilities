using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.Hashing;

public interface IHashAlgorithmProvider
{
    HashTypeKey SupportedHashType { get; }

    int HashData(ReadOnlySpan<byte> source, Span<byte> destination);

    int HashData(Stream source, Span<byte> destination);

    ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellation = default);
}