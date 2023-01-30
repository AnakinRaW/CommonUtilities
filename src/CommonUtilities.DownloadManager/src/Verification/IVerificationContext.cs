namespace AnakinRaW.CommonUtilities.DownloadManager.Verification;

/// <summary>
/// Holds verification information of a downloaded file.
/// </summary>
public interface IVerificationContext
{
    /// <summary>
    /// Contains information used for verification.
    /// </summary>
    object VerificationData { get; }

    /// <summary>
    /// Checks whether <see cref="VerificationData"/> has valid contents.
    /// </summary>
    /// <returns><see langword="true"/> is <see cref="VerificationData"/> is valid; <see langword="false"/> otherwise.</returns>
    bool Verify();
}

/// <summary>
/// Holds typed verification information of a downloaded file.
/// </summary>
/// <typeparam name="T">The type which holds the information for this verifier to perform the necessary checks.</typeparam>
public interface IVerificationContext<out T> : IVerificationContext
{
    /// <summary>
    /// Contains information used for verification.
    /// </summary>
    new T VerificationData { get; }
}