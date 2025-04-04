namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Provides additional options to configure a single download.
/// </summary>
public class DownloadOptions
{
    /// <summary>
    /// Gets or sets the user agent that shall be used to download a file.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Gets or sets the authentication token that shall be used to download a file.
    /// </summary>
    public string? AuthenticationToken { get; init; }
}