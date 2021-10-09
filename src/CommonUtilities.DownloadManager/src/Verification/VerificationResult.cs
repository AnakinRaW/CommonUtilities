namespace Sklavenwalker.CommonUtilities.DownloadManager.Verification;

/// <summary>
/// The status of file verification.
/// </summary>
public enum VerificationResult
{
    /// <summary>
    /// Verification was not processed.
    /// </summary>
    NotVerified,
    /// <summary>
    /// Verification was successful.
    /// </summary>
    Success,
    /// <summary>
    /// Verification failed because <see cref="VerificationContext"/> was invalid.
    /// </summary>
    VerificationContextError,
    /// <summary>
    /// Verification failed.
    /// </summary>
    VerificationFailed,
    /// <summary>
    /// Verification caused a runtime exception.
    /// </summary>
    Exception
}