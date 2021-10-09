using System;
using Sklavenwalker.CommonUtilities.Hashing;
using Validation;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Verification;

/// <summary>
/// Holds verification information of a downloaded file.
/// </summary>
public class VerificationContext
{
    /// <summary>
    /// The expected hash code of the downloaded file.
    /// </summary>
    public byte[] Hash { get; }

    /// <summary>
    /// The <see cref="HashType"/> representing the data in <see cref="Hash"/>.
    /// </summary>
    public HashType HashType { get; }

    /// <summary>
    /// Creates a new <see cref="VerificationContext"/>.
    /// </summary>
    /// <param name="hash">The hash data.</param>
    /// <param name="hashType">The hash type <paramref name="hash"/> represents.</param>
    /// <param name="verify">When set to <see langword="true"/> an integrity check will be performed right away,
    /// whether <paramref name="hash"/> and <paramref name="hashType"/> are compatible.</param>
    /// <exception cref="ArgumentException">When <paramref name="verify"/> is <see langword="true"/>
    /// and <paramref name="hash"/> and <paramref name="hashType"/> mismatch.</exception>
    public VerificationContext(byte[] hash, HashType hashType, bool verify = true)
    {
        Requires.NotNull(hash, nameof(hash));
        Hash = hash;
        HashType = hashType;
        if (verify && !Verify())
            throw new ArgumentException($"Supplied hash length does not match {hashType}");
    }

    internal bool Verify()
    {
        var hashLength = Hash.Length;
        return hashLength.CompareTo((byte)HashType) == 0;
    }
}