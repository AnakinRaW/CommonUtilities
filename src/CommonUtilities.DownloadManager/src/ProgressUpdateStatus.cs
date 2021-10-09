namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Progress report data for downloading a file.
/// </summary>
public class ProgressUpdateStatus
{
    /// <summary>
    /// Bytes read from the source.
    /// </summary>
    public long BytesRead { get; }

    /// <summary>
    /// Bytes written to the output.
    /// </summary>
    public long TotalBytes { get; }

    /// <summary>
    /// Current bit rate.
    /// </summary>
    public double BitRate { get; }

    /// <summary>
    /// The used engine name.
    /// </summary>
    public string? DownloadEngine { get; }

    /// <summary>
    /// Creates new instance with performance data only.
    /// </summary>
    /// <param name="bytesRead">Bytes read from the source.</param>
    /// <param name="totalBytes">Bytes written to the output.</param>
    /// <param name="bitRate">Current bit rate.</param>
    public ProgressUpdateStatus(long bytesRead, long totalBytes, double bitRate)
        : this(null, bytesRead, totalBytes, bitRate)
    {
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="downloadEngine">The download engine.</param>
    /// <param name="bytesRead">Bytes read from the source.</param>
    /// <param name="totalBytes">Bytes written to the output.</param>
    /// <param name="bitRate">Current bit rate.</param>
    public ProgressUpdateStatus(string? downloadEngine, long bytesRead, long totalBytes, double bitRate)
    {
        DownloadEngine = downloadEngine;
        BytesRead = bytesRead;
        TotalBytes = totalBytes;
        BitRate = bitRate;
    }
}