using AnakinRaW.CommonUtilities.Hashing;
using Validation;

namespace AnakinRaW.CommonUtilities.DownloadManager.Verification;


/// <summary>
/// 
/// </summary>
public interface IVerificationContext
{
    /// <summary>
    /// 
    /// </summary>
    object VerificationData { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    bool Verify();
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IVerificationContext<out T> : IVerificationContext
{
    /// <summary>
    /// 
    /// </summary>
    new T VerificationData { get; }
}





/// <summary>
/// 
/// </summary>
public sealed class HashVerificationContext : IVerificationContext<HashingData>
{
    object IVerificationContext.VerificationData => VerificationData;

    /// <summary>
    /// 
    /// </summary>
    public HashingData VerificationData { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool Verify()
    {
        return true;
    }
}

/// <summary>
/// 
/// </summary>
public struct HashingData
{
    /// <summary>
    /// 
    /// </summary>
    public HashType HashType { get; }

    /// <summary>
    /// 
    /// </summary>
    public byte[] Hash { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="hash"></param>
    public HashingData(HashType type, byte[] hash)
    {
        HashType = type;
        Hash = hash;
    }
}


/// <summary>
/// Holds verification information of a downloaded file.
/// </summary>
public sealed class VerificationContext
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
    /// Additional data that may get used for an <see cref="IVerifier"/> for verification.
    /// </summary>
    public object? CustomVerificationData { get; }

    /// <summary>
    /// Creates a new <see cref="VerificationContext"/>.
    /// </summary>
    /// <param name="hash">The hash data.</param>
    /// <param name="hashType">The hash type <paramref name="hash"/> represents.</param>
    /// <param name="customVerificationData">Additional data that may get used for an <see cref="IVerifier"/> for verification.</param>
    public VerificationContext(byte[] hash, HashType hashType, object? customVerificationData = null)
    {
        Requires.NotNull(hash, nameof(hash));
        Hash = hash;
        HashType = hashType;
        CustomVerificationData = customVerificationData;
    }

    /// <summary>
    /// Validates whether <see cref="Hash"/> and <see cref="HashType"/> are compatible.
    /// </summary>
    /// <returns></returns>
    public bool Verify()
    {
        var hashLength = Hash.Length;
        return hashLength.CompareTo((byte)HashType) == 0;
    }
}