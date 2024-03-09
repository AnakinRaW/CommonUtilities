using System;
using System.Diagnostics;

namespace AnakinRaW.CommonUtilities.Hashing;

[DebuggerDisplay("Type: {ToString()} : {HashSize}")]
public readonly struct HashTypeKey : IEquatable<HashTypeKey>
{
    public static readonly HashTypeKey None = default;
    public static readonly HashTypeKey MD5 = new(nameof(MD5), 16);
    public static readonly HashTypeKey SHA1 = new(nameof(SHA1), 20);
    public static readonly HashTypeKey SHA256 = new(nameof(SHA256), 32);
    public static readonly HashTypeKey SHA384 = new(nameof(SHA384), 48);
    public static readonly HashTypeKey SHA512 = new(nameof(SHA512), 64);

#if NET8_0_OR_GREATER
    public static HashTypeKey SHA3_256 = new("SHA3-256", 32);
    public static HashTypeKey SHA3_384 = new("SHA3-384", 48);
    public static HashTypeKey SHA3_512 = new("SHA3-512", 64);
#endif


    public HashTypeKey(string hashType, int hashSize)
    {
        ThrowHelper.ThrowIfNullOrEmpty(hashType);
        HashType = hashType;
        if (hashSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(hashSize), "hashSize must be greater than 0");
        HashSize = hashSize;
    }

    public string HashType { get; }

    public int HashSize { get; }

    public bool Equals(HashTypeKey other)
    {
        return string.Equals(HashType, other.HashType, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is HashTypeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashType.ToLowerInvariant().GetHashCode();
    }

    public override string ToString()
    {
        return HashType ?? "(NONE)";
    }

    public static bool operator ==(HashTypeKey left, HashTypeKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HashTypeKey left, HashTypeKey right)
    {
        return !(left == right);
    }
}