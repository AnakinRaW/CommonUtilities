namespace AnakinRaW.CommonUtilities.DownloadManager.Configuration;

/// <summary>
/// Options how validation at the end of a download shall be handled.
/// </summary>
public enum ValidationPolicy
{
    /// <summary>
    /// Validation will always be skipped.
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