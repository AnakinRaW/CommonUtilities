namespace AnakinRaW.CommonUtilities.DownloadManager.Configuration;

/// <summary>
/// Represents the configuration that is used by an <see cref="IDownloadManager"/>.
/// </summary>
public sealed class DownloadManagerConfiguration
{
    /// <summary>
    /// Returns the default download manager configuration.
    /// </summary>
    /// <remarks>Empty File download is not allowed and verification will be skipped.</remarks>
    public static readonly DownloadManagerConfiguration Default = new();

    /// <summary>
    /// Gets or sets the delay in milliseconds to wait before a download retry. Default is 5 seconds.
    /// </summary>
    public int DownloadRetryDelay { get; init; } = 5000;

    /// <summary>
    /// Gets or sets a value indicating whether empty file downloads (zero bytes) are supported.
    /// By default, empty file downloads are not allowed.
    /// </summary>
    public bool AllowEmptyFileDownload { get; init; }

    /// <summary>
    /// Gets or sets the validation policy for a downloaded file.
    /// </summary>
    public ValidationPolicy ValidationPolicy { get; init; }
    
    /// <summary>
    /// Gets or sets the provider to use for downloading files from the Internet.
    /// </summary>
    public InternetClient InternetClient { get; init; }
}