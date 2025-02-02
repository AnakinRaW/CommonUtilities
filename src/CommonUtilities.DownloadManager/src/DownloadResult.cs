using System;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Information of a completed file download.
/// </summary>
public sealed class DownloadResult
{
    /// <summary>
    /// Gets the size of the downloaded file in bytes.
    /// </summary>
    public long DownloadedSize { get; internal set; }

    /// <summary>
    /// Gets the mean bit rate of the download.
    /// </summary>
    public double BitRate { get; internal set; }

    /// <summary>
    /// Gets the duration of the download.
    /// </summary>
    public TimeSpan DownloadTime { get; internal set; }

    /// <summary>
    /// Gets the name of the used provider which downloaded the file.
    /// </summary>
    public string DownloadProvider { get; internal set; }

    /// <summary>
    /// Gets the actual URI used to download the file.
    /// </summary>
    public Uri? Uri { get; internal set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadResult"/> class with the specified download uri
    /// </summary>
    /// <param name="uri">The origin uri of the download.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    public DownloadResult(Uri uri)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        DownloadProvider = string.Empty;
    }
}