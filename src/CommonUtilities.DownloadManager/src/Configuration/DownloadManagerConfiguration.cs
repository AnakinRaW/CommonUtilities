namespace Sklavenwalker.CommonUtilities.DownloadManager.Configuration;

/// <inheritdoc cref="IDownloadManagerConfiguration"/>
public record DownloadManagerConfiguration : IDownloadManagerConfiguration
{
    /// <summary>
    /// The default download manager configuration.
    /// </summary>
    /// <remarks>Empty File download is not allowed and verification will be skipped.</remarks>
    public static DownloadManagerConfiguration Default = new();

    /// <inheritdoc/>
    public int DownloadRetryDelay { get; set; } = 5000;

    /// <inheritdoc/>
    public bool AllowEmptyFileDownload { get; set; }

    /// <inheritdoc/>
    public VerificationPolicy VerificationPolicy { get; set; }
    
    /// <inheritdoc/>
    public InternetClient InternetClient { get; set; }
}