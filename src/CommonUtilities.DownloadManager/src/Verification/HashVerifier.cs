using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sklavenwalker.CommonUtilities.Hashing;
using Validation;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Verification;

/// <summary>
/// Verifies files based on their Hash.
/// </summary>
public class HashVerifier : IVerifier
{
    private readonly ILogger? _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IHashingService _hashingService;

    /// <summary>
    /// Initializes a new <see cref="HashVerifier"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public HashVerifier(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
    }

    /// <inheritdoc/>
    public VerificationResult Verify(Stream file, VerificationContext verificationContext)
    {
        Requires.NotNull(file, nameof(file));
        if (file is not FileStream fileStream)
            throw new ArgumentException("The stream does not represent a file", nameof(file));
        var path = fileStream.Name;
        return Verify(file, path, verificationContext);

    }

    internal VerificationResult Verify(Stream file, string path, VerificationContext verificationContext)
    {
        if (string.IsNullOrEmpty(path) || !_fileSystem.File.Exists(path))
            throw new FileNotFoundException("Cannot verify a non-existing file.");
        try
        {
            if (!verificationContext.Verify())
                return VerificationResult.VerificationContextError;

            if (verificationContext.HashType == HashType.None)
                return VerificationResult.Success;

            return CompareHashes(file, verificationContext.HashType, verificationContext.Hash)
                ? VerificationResult.Success
                : VerificationResult.VerificationFailed;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, e.Message);
            return VerificationResult.Exception;
        }
    }

    private bool CompareHashes(Stream fileStream, HashType hashType, byte[] expected)
    {
        fileStream.Seek(0L, SeekOrigin.Begin);
        var actualHash = _hashingService.GetStreamHash(fileStream, hashType, true);
        return actualHash.AsSpan().SequenceEqual(expected);
    }
}