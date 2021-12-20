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
    /// <summary>
    /// Represents MD5 with digest size of 16 bytes.
    /// </summary>
    /// <remarks>
    /// Do NOT use this hash type for security relevant code. Use at least SHA256.
    /// </remarks>
    MD5 = 16,
    /// <summary>
    /// Represents SHA1 with digest size of 20 bytes.
    /// </summary>
    /// <remarks>
    /// Do NOT use this hash type for security relevant code. Use at least SHA256.
    /// </remarks>
    Sha1 = 20,
    /// <summary>
    /// Represents SHA256 with digest size of 32 bytes.
    /// </summary>
    Sha256 = 32,
    /// <summary>
    /// Represents SHA384 with digest size of 48 bytes.
    /// </summary>
    Sha384 = 48,
    /// <summary>
    /// Represents SHA512 with digest size of 64 bytes.
    /// </summary>
    Sha512 = 64
}