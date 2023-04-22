using System;
using AnakinRaW.CommonUtilities.Hashing;

namespace AnakinRaW.CommonUtilities.Verification.Hash;

/// <summary>
/// <see cref="IVerificationContext"/> specialized for performing hash-based verification.
/// </summary>
public readonly struct HashVerificationContext : IVerificationContext
{
    /// <summary>
    /// A <see cref="HashVerificationContext"/>, which contains an empty hash and <see cref="HashType"/> is <see cref="Hashing.HashType.None"/>.
    /// </summary>
    public static readonly HashVerificationContext None = new(HashType.None, null);


    /// <summary>
    /// The <see cref="HashType"/> representing the data in <see cref="Hash"/>.
    /// </summary>
    public HashType HashType { get; }

    /// <summary>
    /// The expected hash code of the downloaded file.
    /// </summary>
    public byte[]? Hash { get; }


    /// <summary>
    /// Creates a new instance of <see cref="HashVerificationContext"/> with given verification information.
    /// </summary>
    /// <param name="hash">The hash value.</param>
    /// <param name="hashType">The hash algorithm.</param>
    public HashVerificationContext(HashType hashType, byte[]? hash)
    {
        HashType = hashType;
        Hash = hash;
    }

    /// <summary>
    /// Creates a <see cref="HashVerificationContext"/> object from a hash value.
    /// </summary>
    /// <param name="hash">The hash value to create a <see cref="HashVerificationContext"/> from.</param>
    /// <returns>A <see cref="HashVerificationContext"/> object representing the hash value.</returns>
    /// <exception cref="ArgumentException">Thrown if the hash has an unknown length that does not match any algorithm.</exception>
    public static HashVerificationContext FromHash(byte[]? hash)
    {
        if (hash is null || hash.Length == 0)
            return None;
        if (Enum.IsDefined(typeof(HashType), (byte)hash.Length))
            return new HashVerificationContext((HashType)hash.Length, hash);
        throw new ArgumentException("hash has unknown length");
    }

    /// <summary>
    /// Checks whether <see cref="Hash"/> and <see cref="HashType"/> are compatible.
    /// </summary>
    /// <returns><see langword="true"/> if this instance is valid; <see langword="false"/> otherwise.</returns>
    public bool Verify()
    {
        if (HashType is HashType.None)
            return Hash is null || Hash.Length == 0;
        if (Hash is null)
            return false;
        return Hash.Length.CompareTo((byte)HashType) == 0;
    }
}