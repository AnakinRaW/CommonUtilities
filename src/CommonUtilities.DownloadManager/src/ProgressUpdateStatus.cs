namespace AnakinRaW.CommonUtilities.DownloadManager;

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
    /// The used provider name.
    /// </summary>
    public string? DownloadProvider { get; }

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
    /// <param name="downloadProvider">The download provider.</param>
    /// <param name="bytesRead">Bytes read from the source.</param>
    /// <param name="totalBytes">Bytes written to the output.</param>
    /// <param name="bitRate">Current bit rate.</param>
    public ProgressUpdateStatus(string? downloadProvider, long bytesRead, long totalBytes, double bitRate)
    {
        DownloadProvider = downloadProvider;
        BytesRead = bytesRead;
        TotalBytes = totalBytes;
        BitRate = bitRate;
    }
}