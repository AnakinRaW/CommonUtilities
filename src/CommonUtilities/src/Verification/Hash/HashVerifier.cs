using System;
using System.IO;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.CommonUtilities.Verification.Hash;

/// <summary>
/// Verifies files based on their Hash.
/// </summary>
public class HashVerifier : VerifierBase<HashVerificationContext>
{
    private readonly IHashingService _hashingService;

    /// <summary>
    /// Initializes a new <see cref="HashVerifier"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public HashVerifier(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
    }

    /// <inheritdoc/>
    protected override VerificationResult VerifyCore(Stream data, HashVerificationContext verificationContext)
    {
        if (!verificationContext.Verify())
            return new VerificationResult(VerificationResultStatus.VerificationContextError);

        if (verificationContext.HashType == HashType.None)
            return new VerificationResult(VerificationResultStatus.Success);

        if (verificationContext.Hash is null)
            throw new InvalidOperationException("Expected Hash data to be non-null");

        return CompareHashes(data, verificationContext.HashType, verificationContext.Hash)
            ? new VerificationResult(VerificationResultStatus.Success)
            : new VerificationResult(VerificationResultStatus.VerificationFailed);
    }

    private bool CompareHashes(Stream fileStream, HashType hashType, ReadOnlySpan<byte> expected)
    {
        fileStream.Seek(0L, SeekOrigin.Begin);
        var actualHash = _hashingService.GetStreamHash(fileStream, hashType);
        return actualHash.AsSpan().SequenceEqual(expected);
    }
}