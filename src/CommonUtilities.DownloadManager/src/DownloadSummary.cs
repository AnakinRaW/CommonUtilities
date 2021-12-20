using System;
using Sklavenwalker.CommonUtilities.DownloadManager.Verification;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Summary information of a completed file download.
/// </summary>
public class DownloadSummary
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
    /// Name of the used engine which downloaded the file.
    /// </summary>
    public string DownloadEngine { get; internal set; }

    /// <summary>
    /// Proxy resolution information.
    /// </summary>
    public ProxyResolution? ProxyResolution { get; internal set; }

    /// <summary>
    /// The actual URI used to download the file.
    /// </summary>
    public string FinalUri { get; internal set; }

    /// <summary>
    /// The verification result of the download.
    /// </summary>
    public VerificationResult ValidationResult { get; internal set; }

    /// <summary>
    /// Creates an empty <see cref="DownloadSummary"/>
    /// </summary>
    public DownloadSummary()
        : this(string.Empty, 0L, 0.0, TimeSpan.Zero, default)
    {
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="downloadEngine">The used engine name.</param>
    /// <param name="downloadSize">The downloaded bytes.</param>
    /// <param name="bitRate">Mean downloading rate.</param>
    /// <param name="downloadTime">The download duration.</param>
    /// <param name="validationResult">The verification result.</param>
    public DownloadSummary(string downloadEngine, long downloadSize, double bitRate, TimeSpan downloadTime, VerificationResult validationResult)
    {
        DownloadEngine = downloadEngine;
        DownloadedSize = downloadSize;
        BitRate = bitRate;
        DownloadTime = downloadTime;
        ProxyResolution = null;
        FinalUri = string.Empty;
        ValidationResult = validationResult;
    }
}