namespace Sklavenwalker.CommonUtilities.Hashing;

/// <summary>
/// Supported Hashing Algorithms by the <see cref="IHashingService"/>.
/// <remarks>The value of each enum represents the digest size in bytes.</remarks>
/// </summary>
public enum HashType : byte
{
    /// <summary>
    /// Special entry used to represent an undefined default value.
    /// </summary>
    None = 0,
    MD5 = 16,
    Sha1 = 20,
    Sha256 = 32,
    Sha384 = 48,
    Sha512 = 64
}