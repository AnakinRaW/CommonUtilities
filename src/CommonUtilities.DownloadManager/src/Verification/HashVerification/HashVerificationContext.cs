using System;
using AnakinRaW.CommonUtilities.Hashing;
using Validation;

namespace AnakinRaW.CommonUtilities.DownloadManager.Verification.HashVerification;

/// <summary>
/// <see cref="IVerificationContext"/> specialized for performing hash-based verification.
/// </summary>
public sealed class HashVerificationContext : IVerificationContext<HashingData>
{
    /// <summary>
    /// A <see cref="HashVerificationContext"/>, which contains an empty <see cref="HashingData"/>.
    /// </summary>
    public static readonly HashVerificationContext None = new(new HashingData(HashType.None, Array.Empty<byte>()));

    object IVerificationContext.VerificationData => VerificationData;

    /// <inheritdoc />
    public HashingData VerificationData { get; }

    /// <summary>
    /// Creates a new instance of <see cref="HashVerificationContext"/> with given verification information.
    /// </summary>
    /// <param name="verificationData">The hash information for this context.</param>
    public HashVerificationContext(HashingData verificationData)
    {
        VerificationData = verificationData;
    }

    /// <summary>
    /// Creates a new instance of <see cref="HashVerificationContext"/> with given verification information.
    /// </summary>
    /// <param name="hash">The hash value.</param>
    /// <param name="hashType">The hash algorithm.</param>
    public HashVerificationContext(byte[] hash, HashType hashType)
    {
        if (hash == null) 
            throw new ArgumentNullException(nameof(hash));
        VerificationData = new HashingData(hashType, hash);
    }

    /// <summary>
    /// Checks whether <see cref="HashingData.Hash"/> and <see cref="HashingData.HashType"/> of <see cref="VerificationData"/> are compatible.
    /// </summary>
    /// <returns><see langword="true"/> is <see cref="VerificationData"/> is valid; <see langword="false"/> otherwise.</returns>
    public bool Verify()
    {
        return VerificationData.Hash.Length.CompareTo((byte)VerificationData.HashType) == 0;
    }
}