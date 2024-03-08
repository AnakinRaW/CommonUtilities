using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.CommonUtilities.DownloadManager.Validation;

/// <summary>
/// A hash-based download validator
/// </summary>
public sealed class HashDownloadValidator : IDownloadValidator
{
    private readonly byte[]? _hash;
    private readonly HashType _hashType;
    private readonly IHashingService _hashingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashDownloadValidator"/> class.
    /// </summary>
    /// <param name="hash">The expected hash.</param>
    /// <param name="hashType">The hash type.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public HashDownloadValidator(byte[]? hash, HashType hashType, IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        var size = hash?.Length ?? 0;
        if (size != (int)hashType)
            throw new ArgumentException("Hash value and hash type do not match.", nameof(hash));

        _hash = (byte[]?)hash?.Clone() ?? null;
        _hashType = hashType;
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
    }

    /// <inheritdoc />
    public Task<bool> Validate(Stream stream, long downloadedBytes, CancellationToken token = default)
    {
        if (stream == null) 
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanSeek)
            throw new NotSupportedException("Stream must be seekable.");

        if (_hashType == HashType.None)
            return Task.FromResult(true);

        stream.Seek(-downloadedBytes, SeekOrigin.Current);
        var actualHash = _hashingService.GetStreamHash(stream, _hashType);

        return Task.FromResult(_hash!.SequenceEqual(actualHash));
    }
}