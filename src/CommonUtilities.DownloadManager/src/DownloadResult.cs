﻿using System;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Information of a completed file download.
/// </summary>
public sealed class DownloadResult
{
    /// <summary>
    /// The size of the downloaded file in bytes.
    /// </summary>
    public long DownloadedSize { get; internal set; }

    /// <summary>
    /// The mean bit rate of the download.
    /// </summary>
    public double BitRate { get; internal set; }

    /// <summary>
    /// The duration of the download.
    /// </summary>
    public TimeSpan DownloadTime { get; internal set; }

    /// <summary>
    /// Name of the used provider which downloaded the file.
    /// </summary>
    public string DownloadProvider { get; internal set; }

    /// <summary>
    /// The actual URI used to download the file.
    /// </summary>
    public string Uri { get; internal set; }

    /// <summary>
    /// Creates an empty <see cref="DownloadResult"/>
    /// </summary>
    internal DownloadResult() : this(string.Empty, 0L, 0.0, TimeSpan.Zero)
    {
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="downloadProvider">The used provider name.</param>
    /// <param name="downloadSize">The downloaded bytes.</param>
    /// <param name="bitRate">Mean downloading rate.</param>
    /// <param name="downloadTime">The download duration.</param>
    internal DownloadResult(string downloadProvider, long downloadSize, double bitRate, TimeSpan downloadTime)
    {
        DownloadProvider = downloadProvider;
        DownloadedSize = downloadSize;
        BitRate = bitRate;
        DownloadTime = downloadTime;
        Uri = string.Empty;
    }
}