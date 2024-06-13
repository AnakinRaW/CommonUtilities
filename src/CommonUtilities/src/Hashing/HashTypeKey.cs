using System;
using System.Diagnostics;

namespace AnakinRaW.CommonUtilities.Hashing;

/// <summary>
/// Identifies a hash algorithm and its size value.
/// </summary>
/// <remarks>
/// The <see cref="HashTypeKey"/> structure includes some static properties that return predefined hash keys. Hash keys are case-insensitive.
/// </remarks>
[DebuggerDisplay("Type: {ToString()} : {HashSize}")]
public readonly struct HashTypeKey : IEquatable<HashTypeKey>
{
    /// <summary>
    /// Gets a <see cref="HashTypeKey"/> that represents no algorithm.
    /// </summary>
    public static readonly HashTypeKey None = default;

    /// <summary>
    /// Gets a <see cref="HashTypeKey"/> that represents "MD5".
    /// </summary>
    public static readonly HashTypeKey MD5 = new(nameof(MD5), 16);

    /// <summary>
    /// Gets a <see cref="HashTypeKey"/> that represents "SHA1".
    /// </summary>
    public static readonly HashTypeKey SHA1 = new(nameof(SHA1), 20);

    /// <summary>
    /// Gets a <see cref="HashTypeKey"/> that represents "SHA256".
    /// </summary>
    public static readonly HashTypeKey SHA256 = new(nameof(SHA256), 32);

    /// <summary>
    /// Gets a <see cref="HashTypeKey"/> that represents "SHA384".
    /// </summary>
    public static readonly HashTypeKey SHA384 = new(nameof(SHA384), 48);

    /// <summary>
    /// Gets a <see cref="HashTypeKey"/> that represents "SHA512".
    /// </summary>
    public static readonly HashTypeKey SHA512 = new(nameof(SHA512), 64);

#if NET8_0_OR_GREATER
    /// <summary>
    /// Gets a <see cref="HashTypeKey"/> that represents "SHA3-256".
    /// </summary>
    public static HashTypeKey SHA3_256 = new("SHA3-256", 32);

    /// <summary>
    /// Gets a <see cref="HashTypeKey"/> that represents "SHA3-384".
    /// </summary>
    public static HashTypeKey SHA3_384 = new("SHA3-384", 48);

    /// <summary>
    /// Gets a <see cref="HashTypeKey"/> that represents "SHA3-512".
    /// </summary>
    public static HashTypeKey SHA3_512 = new("SHA3-512", 64);
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTypeKey"/> structure with a name and size.
    /// </summary>
    /// <param name="name">The key of the hash algorithm.</param>
    /// <param name="hashSize">The size in bytes of the algorithm.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="hashSize"/> is 0 or negative.</exception>
    public HashTypeKey(string name, int hashSize)
    {
        ThrowHelper.ThrowIfNullOrEmpty(name);
        Name = name;
        if (hashSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(hashSize), "hashSize must be greater than 0");
        HashSize = hashSize;
    }

    /// <summary>
    /// Gets the string representation of the key.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the size in bytes of the algorithm's have values.
    /// </summary>
    public int HashSize { get; }

    /// <summary>
    /// Returns a value that indicates whether two <see cref="HashTypeKey"/> values are equal.
    /// </summary>
    /// <param name="other">The object to compare with the current instance.</param>
    /// <returns><see langowrd="true"/> if obj is a <see cref="HashTypeKey"/> object and its <see cref="Name"/> property is equal to that of the current instance. The comparison is ordinal and case-insensitive.</returns>
    public bool Equals(HashTypeKey other)
    {
        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a value that indicates whether two <see cref="HashTypeKey"/> values are equal.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><see langowrd="true"/> if the <see cref="Name"/> property of other is equal to that of the current instance. The comparison is ordinal and case-insensitive.</returns>
    public override bool Equals(object? obj)
    {
        return obj is HashTypeKey other && Equals(other);
    }

    /// <summary>
    /// Returns the hash code for the current instance.
    /// </summary>
    /// <returns>The hash code for the current instance, or 0 if this is <see cref="None"/>.</returns>
    public override int GetHashCode()
    {
        return Name?.ToUpperInvariant().GetHashCode() ?? 0;
    }

    /// <summary>
    /// Returns the string representation of the current <see cref="HashTypeKey"/> instance.
    /// </summary>
    /// <returns>The string representation of the current <see cref="HashTypeKey"/> instance.</returns>
    public override string ToString()
    {
        return Name ?? "(NONE)";
    }

    /// <summary>
    /// Determines whether two specified <see cref="HashTypeKey"/> objects are equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><see langowrd="true"/> if both <paramref name="left"/> and <paramref name="right"/> have the same <see cref="Name"/> value; otherwise, <see langowrd="false"/>.</returns>
    public static bool operator ==(HashTypeKey left, HashTypeKey right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two specified <see cref="HashTypeKey"/> objects are not equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><see langowrd="true"/> if both <paramref name="left"/> and <paramref name="right"/> do not have the same <see cref="Name"/> value; otherwise, <see langowrd="false"/>.</returns>
    public static bool operator !=(HashTypeKey left, HashTypeKey right)
    {
        return !(left == right);
    }
}