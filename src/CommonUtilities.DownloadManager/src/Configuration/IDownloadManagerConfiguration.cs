using System.Net.Http;

namespace AnakinRaW.CommonUtilities.DownloadManager.Configuration;

/// <summary>
/// Configures the behavior of an <see cref="IDownloadManager"/> class.
/// </summary>
public interface IDownloadManagerConfiguration
{
    /// <summary>
    /// Gets the delay in ms between a download retry.
    /// </summary>
    int DownloadRetryDelay { get; }

    /// <summary>
    /// Gets a value that specifies whether it's legal to download empty files.
    /// An <see cref="IDownloadManager"/> may react with an error when downloading
    /// an empty file with this options set to <see langword="false"/>.
    /// </summary>
    bool AllowEmptyFileDownload { get; }

    /// <summary>
    /// Gets a value that specifies how verification after the download shall be handled.
    /// </summary>
    ValidationPolicy ValidationPolicy { get; }

    /// <summary>
    /// Gets the <see cref="Configuration.InternetClient"/> implementation which shall get used.
    /// Default is <see cref="HttpClient"/>
    /// </summary>
    InternetClient InternetClient { get; }
}