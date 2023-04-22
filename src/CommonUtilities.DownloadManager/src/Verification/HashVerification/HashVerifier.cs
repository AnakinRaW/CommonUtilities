using System;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.CommonUtilities.DownloadManager.Verification.HashVerification;

/// <summary>
/// Verifies files based on their Hash.
/// </summary>
public class HashVerifier : IVerifier<HashingData>
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="file"></param>
    /// <param name="verificationContext"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public VerificationResult Verify(Stream file, IVerificationContext<HashingData> verificationContext)
    {
        Requires.NotNull(file, nameof(file));
        var path = file.GetPathFromStream();
        return Verify(file, path, verificationContext);
    }

    /// <inheritdoc/>
    VerificationResult IVerifier.Verify(Stream file, IVerificationContext verificationContext)
    {
        return Verify(file, (IVerificationContext<HashingData>)verificationContext);
    }

    internal VerificationResult Verify(Stream file, string path, IVerificationContext<HashingData> verificationContext)
    {
        if (string.IsNullOrEmpty(path) || !_fileSystem.File.Exists(path))
            throw new FileNotFoundException("Cannot verify a non-existing file.");
        try
        {
            if (!verificationContext.Verify())
                return VerificationResult.VerificationContextError;

            if (verificationContext.VerificationData.HashType == HashType.None)
                return VerificationResult.Success;

            return CompareHashes(file, verificationContext.VerificationData.HashType, verificationContext.VerificationData.Hash)
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
        var actualHash = _hashingService.GetStreamHash(fileStream, hashType);
        return actualHash.AsSpan().SequenceEqual(expected);
    }
}