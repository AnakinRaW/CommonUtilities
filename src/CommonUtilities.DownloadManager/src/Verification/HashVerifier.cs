using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sklavenwalker.CommonUtilities.Hashing;
using Validation;
#if !(NET || NETSTANDARD2_1)
using System.Linq;
#endif

namespace Sklavenwalker.CommonUtilities.DownloadManager.Verification;

internal class HashVerifier : IVerifier
{
    private readonly ILogger? _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IHashingService _hashingService;

    public HashVerifier(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _fileSystem = serviceProvider.GetService<IFileSystem>() ?? new FileSystem();
        _hashingService = serviceProvider.GetService<IHashingService>() ?? new HashingService();
    }

    public VerificationResult Verify(Stream file, VerificationContext verificationContext)
    {
        Requires.NotNull(file, nameof(file));
        if (file is not FileStream fileStream)
            throw new ArgumentException("The stream does not represent a file", nameof(file));
        var path = fileStream.Name;
        if (string.IsNullOrEmpty(path) || !_fileSystem.File.Exists(path))
            throw new InvalidOperationException("Cannot verify a non-existing file.");
        try
        {
            if (!verificationContext.Verify())
                return VerificationResult.VerificationContextError;

            if (verificationContext.HashType == HashType.None)
                return VerificationResult.Success;

            return CompareHashes(fileStream, verificationContext.HashType, verificationContext.Hash)
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
#if NET || NETSTANDARD2_1
        return actualHash.AsSpan().SequenceEqual(expected);
#else
        return actualHash.SequenceEqual(expected);
#endif
    }
}