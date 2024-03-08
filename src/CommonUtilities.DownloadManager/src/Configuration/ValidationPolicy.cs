namespace AnakinRaW.CommonUtilities.DownloadManager.Configuration;

/// <summary>
/// Options how verification at the end of a download shall be handled.
/// </summary>
public enum ValidationPolicy
{
    /// <summary>
    /// Verification will always be skipped.
    /// </summary>
    NoValidation,
    /// <summary>
    /// Validation is optional.
    /// </summary>
    Optional,
    /// <summary>
    /// Validation is required.
    /// </summary>
    Required,
}