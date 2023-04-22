namespace AnakinRaW.CommonUtilities.DownloadManager.Configuration;

/// <summary>
/// Options how verification at the end of a download shall be handled.
/// </summary>
public enum VerificationPolicy
{
    /// <summary>
    /// Verification will always be skipped.
    /// </summary>
    Skip,
    /// <summary>
    /// Verification will be skipped if no <see cref="Verification.IVerificationContext"/> is provided
    /// or the <see cref="Verification.IVerificationContext"/> has invalid data.
    /// </summary>
    SkipWhenNoContextOrBroken,
    /// <summary>
    /// Verification will be skipped if no <see cref="Verification.IVerificationContext"/> is provided.
    /// </summary>
    Optional,
    /// <summary>
    /// Verification is enforced.
    /// </summary>
    Enforce,
}