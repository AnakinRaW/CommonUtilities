namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Represents a progress report data for downloading a file.
/// </summary>
public sealed class ProgressUpdateStatus
{
    /// <summary>
    /// Gets the bytes read from the source.
    /// </summary>
    public long BytesRead { get; }

    /// <summary>
    /// Gets the bytes written to the output.
    /// </summary>
    public long TotalBytes { get; }

    /// <summary>
    /// Gets the current bit rate.
    /// </summary>
    public double BitRate { get; }

    /// <summary>
    /// Gets the used provider name.
    /// </summary>
    public string? DownloadProvider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressUpdateStatus"/> class of the specified download data.
    /// </summary>
    /// <param name="bytesRead">The bytes read from the source.</param>
    /// <param name="totalBytes">The bytes written to the output.</param>
    /// <param name="bitRate">The current bit rate.</param>
    public ProgressUpdateStatus(long bytesRead, long totalBytes, double bitRate)
        : this(null, bytesRead, totalBytes, bitRate)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressUpdateStatus"/> class of the specified provider name and download data.
    /// </summary>
    /// <param name="downloadProvider">The download provider.</param>
    /// <param name="bytesRead">The bytes read from the source.</param>
    /// <param name="totalBytes">The bytes written to the output.</param>
    /// <param name="bitRate">The current bit rate.</param>
    public ProgressUpdateStatus(string? downloadProvider, long bytesRead, long totalBytes, double bitRate)
    {
        DownloadProvider = downloadProvider;
        BytesRead = bytesRead;
        TotalBytes = totalBytes;
        BitRate = bitRate;
    }
}