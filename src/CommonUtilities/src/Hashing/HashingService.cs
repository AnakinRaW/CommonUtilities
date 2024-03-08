using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.Hashing;

/// <inheritdoc cref="IHashingService"/>
public class HashingService : IHashingService
{
    /// <inheritdoc/>
    public byte[] GetFileHash(IFileInfo file, HashType hashType)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
        using var stream =
            file.FileSystem.FileStream.New(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        return GetStreamHash(stream, hashType);
    }

    /// <inheritdoc/>
    public byte[] GetStreamHash(Stream stream, HashType hashType)
    {
        if (stream == null) 
            throw new ArgumentNullException(nameof(stream));

        return HashFileInternal(stream, GetAlgorithm(hashType));
    }

    private static byte[] HashFileInternal(Stream inputStream, HashAlgorithm algorithm)
    {
        if (!inputStream.CanRead)
            throw new InvalidOperationException("Cannot hash unreadable stream");
        if (!inputStream.CanSeek)
            throw new InvalidOperationException("Cannot hash stream.");
        inputStream.Position = 0;
        using (algorithm)
            return algorithm.ComputeHash(inputStream);
    }

    private static HashAlgorithm GetAlgorithm(HashType hashType)
    {
        return hashType switch
        {
            HashType.MD5 => MD5.Create(),
            HashType.Sha1 => SHA1.Create(),
            HashType.Sha256 => SHA256.Create(),
            HashType.Sha512 => SHA512.Create(),
            HashType.Sha384 => SHA384.Create(),
            _ => throw new NotSupportedException("Unable to find a hashing algorithm")
        };
    }

#if NET
    /// <inheritdoc/>
    public async Task<byte[]> HashFileAsync(IFileInfo file, HashType hashType)
    {
        if (file == null) 
            throw new ArgumentNullException(nameof(file));
        await using var fs = file.OpenRead();
        return await HashInternalAsync(fs, hashType).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<byte[]> GetStreamHashAsync(Stream stream, HashType hashType)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        return await HashInternalAsync(stream, hashType).ConfigureAwait(false);
    }

    private static async Task<byte[]> HashInternalAsync(Stream stream, HashType hashType)
    {
        using var algorithm = GetAlgorithm(hashType);
        return await algorithm.ComputeHashAsync(stream).ConfigureAwait(false);
    }
#endif
}