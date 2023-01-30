using System;
using AnakinRaW.CommonUtilities.Hashing;

namespace AnakinRaW.CommonUtilities.DownloadManager.Verification.HashVerification;

/// <summary>
/// Contains a hash value and its algorithm information.
/// </summary>
public class HashingData
{
    /// <summary>
    /// The <see cref="HashType"/> representing the data in <see cref="Hash"/>.
    /// </summary>
    public HashType HashType { get; }

    /// <summary>
    /// The expected hash code of the downloaded file.
    /// </summary>
    public byte[] Hash { get; }

    /// <summary>
    /// Creates a new instance of <see cref="HashingData"/>
    /// </summary>
    /// <param name="type">The used algorithm of this instance.</param>
    /// <param name="hash">The hash value.</param>
    public HashingData(HashType type, byte[] hash)
    {
        HashType = type;
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
    }
}