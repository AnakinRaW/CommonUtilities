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
    public string Uri { get; internal set; }

    internal DownloadResult() : this(string.Empty, 0L, 0.0, TimeSpan.Zero)
    {
    }

    internal DownloadResult(string downloadProvider, long downloadSize, double bitRate, TimeSpan downloadTime)
    {
        DownloadProvider = downloadProvider;
        DownloadedSize = downloadSize;
        BitRate = bitRate;
        DownloadTime = downloadTime;
        Uri = string.Empty;
    }
}