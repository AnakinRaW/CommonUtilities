namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Represents a progress report data for downloading a file.
/// </summary>
public sealed class DownloadUpdate
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
    public string? DownloadProvider { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadUpdate"/> class of the specified download data.
    /// </summary>
    /// <param name="bytesRead">The bytes read from the source.</param>
    /// <param name="totalBytes">The bytes written to the output.</param>
    /// <param name="bitRate">The current bit rate.</param>
    public DownloadUpdate(long bytesRead, long totalBytes, double bitRate)
    {
        BytesRead = bytesRead;
        TotalBytes = totalBytes;
        BitRate = bitRate;
    }
}