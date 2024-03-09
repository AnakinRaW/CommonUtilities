using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.Hashing;

public interface IHashingService
{
    byte[] GetHash(IFileInfo file, HashTypeKey hashType);

    int GetHash(IFileInfo file, Span<byte> destination, HashTypeKey hashType);

    byte[] GetHash(Stream source, HashTypeKey hashType);

    int GetHash(Stream source, Span<byte> destination, HashTypeKey hashType);

    int GetHash(ReadOnlySpan<byte> source, Span<byte> destination, HashTypeKey hashType);

    byte[] GetHash(byte[] source, HashTypeKey hashType);

    byte[] GetHash(string stringData, Encoding encoding, HashTypeKey hashType);

    int GetHash(string stringData, Encoding encoding, Span<byte> destination, HashTypeKey hashType);


    ValueTask<byte[]> GetHashAsync(IFileInfo file, HashTypeKey hashType, CancellationToken cancellationToken = default);

    ValueTask<int> GetHashAsync(IFileInfo file, Memory<byte> destination, HashTypeKey hashType, CancellationToken cancellationToken = default);

    ValueTask<byte[]> GetHashAsync(Stream source, HashTypeKey hashType, CancellationToken cancellationToken = default);

    ValueTask<int> GetHashAsync(Stream source, Memory<byte> destination, HashTypeKey hashType, CancellationToken cancellationToken = default);
}