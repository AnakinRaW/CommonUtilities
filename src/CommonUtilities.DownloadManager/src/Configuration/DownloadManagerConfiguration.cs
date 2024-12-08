namespace AnakinRaW.CommonUtilities.DownloadManager.Configuration;

/// <inheritdoc cref="IDownloadManagerConfiguration"/>
public record DownloadManagerConfiguration : IDownloadManagerConfiguration
{
    /// <summary>
    /// Returns the default download manager configuration.
    /// </summary>
    /// <remarks>Empty File download is not allowed and verification will be skipped.</remarks>
    public static readonly DownloadManagerConfiguration Default = new();

    /// <inheritdoc/>
    public int DownloadRetryDelay { get; init; } = 5000;

    /// <inheritdoc/>
    public bool AllowEmptyFileDownload { get; init; }

    /// <inheritdoc/>
    public ValidationPolicy ValidationPolicy { get; init; }
    
    /// <inheritdoc/>
    public InternetClient InternetClient { get; init; }
}