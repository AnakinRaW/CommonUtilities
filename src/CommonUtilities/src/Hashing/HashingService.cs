﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Extensions;
using AnakinRaW.CommonUtilities.Hashing.Providers;
using Microsoft.Extensions.DependencyInjection;
#if NET8_0_OR_GREATER
using System.Security.Cryptography;
#endif

namespace AnakinRaW.CommonUtilities.Hashing;

public sealed class HashingService : IHashingService
{
    private readonly Dictionary<HashTypeKey, IHashAlgorithmProvider> _providers = new();

    private readonly IFileSystem _fileSystem;

    public HashingService(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));

        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        AddDefaultProviders();

        var customProviders = serviceProvider.GetServices<IHashAlgorithmProvider>();
        foreach (var customProvider in customProviders)
        {
#if NET || NETSTANDARD2_1
            if (!_providers.TryAdd(customProvider.SupportedHashType, customProvider))
                throw new InvalidOperationException($"Hash provider with key '{customProvider.SupportedHashType}' is already registered.");
#else
            if (_providers.ContainsKey(customProvider.SupportedHashType))
                throw new InvalidOperationException($"Hash provider with key '{customProvider.SupportedHashType}' is already registered.");
            _providers[customProvider.SupportedHashType] = customProvider;
#endif
        }
    }

    private void AddDefaultProviders()
    { 
        _providers[HashTypeKey.MD5] = new MD5HashProvider();
        _providers[HashTypeKey.SHA1] = new SHA1HashProvider();
        _providers[HashTypeKey.SHA256] = new SHA256HashProvider();
        _providers[HashTypeKey.SHA384] = new SHA384HashProvider();
        _providers[HashTypeKey.SHA512] = new SHA512HashProvider();

#if NET8_0_OR_GREATER
        if (SHA3_256.IsSupported)
            _providers[HashTypeKey.SHA3_256] = new SHA3_256HashProvider();
        if (SHA3_384.IsSupported)
            _providers[HashTypeKey.SHA3_384] = new SHA3_384HashProvider();
        if (SHA3_512.IsSupported)
            _providers[HashTypeKey.SHA3_512] = new SHA3_512HashProvider();
#endif
    }

    public byte[] GetHash(IFileInfo file, HashTypeKey hashType)
    {
        if (file == null) 
            throw new ArgumentNullException(nameof(file));
        using var fileStream = _fileSystem.FileStream.New(file.FullName, FileMode.Open, FileAccess.Read);
        return GetHash(fileStream, hashType);
    }

    public int GetHash(IFileInfo file, Span<byte> destination, HashTypeKey hashType)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
        using var fileStream = _fileSystem.FileStream.New(file.FullName, FileMode.Open, FileAccess.Read);
        return GetHash(fileStream, destination, hashType);
    }

    public byte[] GetHash(Stream source, HashTypeKey hashType)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));

        var hashValue = new byte[hashType.HashSize];
        GetHash(source, hashValue, hashType);
        return hashValue;
    }

    public int GetHash(Stream source, Span<byte> destination, HashTypeKey hashType)
    {
        var provider = GetProvider(hashType);
        var bytesRead = provider.HashData(source, destination);

        if (bytesRead != hashType.HashSize)
            throw new InvalidOperationException("The calculated hash is not of the correct size.");

        return bytesRead;
    }

    public int GetHash(ReadOnlySpan<byte> source, Span<byte> destination, HashTypeKey hashType)
    {
        var provider = GetProvider(hashType); 
        var bytesRead = provider.HashData(source, destination);

        if (bytesRead != hashType.HashSize)
            throw new InvalidOperationException("The calculated hash is not of the correct size.");

        return bytesRead;
    }

    private IHashAlgorithmProvider GetProvider(HashTypeKey hashType)
    {
        if (_providers.TryGetValue(hashType, out var provider))
            return provider;
        throw new HashProviderNotFoundException(hashType);
    }

    public byte[] GetHash(byte[] source, HashTypeKey hashType)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        
        var hashValue = new byte[hashType.HashSize];
        GetHash(new ReadOnlySpan<byte>(source), hashValue, hashType);
        return hashValue;
    }

    public byte[] GetHash(string stringData, Encoding encoding, HashTypeKey hashType)
    {
        if (stringData == null)
            throw new ArgumentNullException(nameof(stringData));
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));

        var hashValue = new byte[hashType.HashSize];
        GetHash(stringData, encoding, hashValue, hashType);
        return hashValue;
    }

    public int GetHash(string stringData, Encoding encoding, Span<byte> destination, HashTypeKey hashType)
    {
        if (stringData == null)
            throw new ArgumentNullException(nameof(stringData));
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));

        var stringSpan = stringData.AsSpan();
        var maxByteSize = encoding.GetMaxByteCount(stringSpan.Length);

        byte[]? encodedBytes = null;
        try
        {
            var buffer = maxByteSize > 256 ? encodedBytes = ArrayPool<byte>.Shared.Rent(maxByteSize) : stackalloc byte[maxByteSize];
            var bytesToHash = encoding.GetBytesReadOnly(stringSpan, buffer);

            return GetHash(bytesToHash, destination, hashType);
        }
        finally
        {
            if (encodedBytes is not null)
                ArrayPool<byte>.Shared.Return(encodedBytes);
        }
    }

    public async ValueTask<byte[]> GetHashAsync(IFileInfo file, HashTypeKey hashType, CancellationToken cancellationToken = default)
    {
        if (file == null) 
            throw new ArgumentNullException(nameof(file));

        var buffer = new byte[hashType.HashSize];
        await GetHashAsync(file, buffer, hashType, cancellationToken);
        return buffer;
    }

    public async ValueTask<int> GetHashAsync(IFileInfo file, Memory<byte> destination, HashTypeKey hashType,
        CancellationToken cancellationToken = default)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

#if NETSTANDARD2_1 || NET
        var fileStream = file.OpenRead();
        await using (fileStream.ConfigureAwait(false))
#else
        using var fileStream = file.OpenRead();
#endif
        return await GetHashAsync(fileStream, destination, hashType, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<byte[]> GetHashAsync(Stream source, HashTypeKey hashType, CancellationToken cancellationToken = default)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));

        var hashValue = new byte[hashType.HashSize];
        await GetHashAsync(source, hashValue, hashType, cancellationToken);
        return hashValue;
    }

    public async ValueTask<int> GetHashAsync(Stream source, Memory<byte> destination, HashTypeKey hashType,
        CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(hashType);
        var bytesRead = await provider.HashDataAsync(source, destination, cancellationToken);

        if (bytesRead != hashType.HashSize)
            throw new InvalidOperationException("The calculated hash is not of the correct size.");

        return bytesRead;
    }
}