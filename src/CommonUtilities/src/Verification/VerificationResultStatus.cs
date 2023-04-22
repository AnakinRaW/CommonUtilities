namespace AnakinRaW.CommonUtilities.Verification;

/// <summary>
/// The status of verification.
/// </summary>
public enum VerificationResultStatus
{
    /// <summary>
    /// Verification was not processed.
    /// </summary>
    /// <remarks>This status shall only be used for the <see cref="IVerificationManager"/>.</remarks>
    NotVerified,
    /// <summary>
    /// Verification was successful.
    /// </summary>
    Success,
    /// <summary>
    /// Verification failed because <see cref="IVerificationContext"/> was invalid.
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