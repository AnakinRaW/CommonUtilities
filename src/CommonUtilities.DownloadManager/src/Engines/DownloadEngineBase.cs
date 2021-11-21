using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Engines;

/// <summary>
/// Base implementation for an <see cref="IDownloadEngine"/>.
/// </summary>
public abstract class DownloadEngineBase : DisposableObject, IDownloadEngine {
    
    private readonly DownloadSource[] _supportedSources;

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// Initializes an <see cref="IDownloadEngine"/> instance.
    /// </summary>
    /// <param name="name">The name of the concrete instance.</param>
    /// <param name="supportedSources">The supported download locations by this instance.</param>
    protected DownloadEngineBase(string name, DownloadSource[] supportedSources)
    {
        Name = name;
        _supportedSources = supportedSources;
    }

    /// <inheritdoc/>
    public bool IsSupported(DownloadSource source)
    {
        return _supportedSources.Contains(source);
    }

    /// <inheritdoc/>
    public DownloadSummary Download(Uri uri, Stream outputStream, ProgressUpdateCallback progress,
        CancellationToken cancellationToken)
    {
        return DownloadWithBitRate(uri, outputStream, progress, cancellationToken);
    }

    /// <summary>
    /// Concrete implementation for downloading a file.
    /// </summary>
    /// <param name="uri">The location of the source file.</param>
    /// <param name="outputStream">The output stream.</param>
    /// <param name="progress">Progress with already updated performance data.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns></returns>
    protected abstract DownloadSummary DownloadCore(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken);

    private DownloadSummary DownloadWithBitRate(
        Uri uri,
        Stream outputStream,
        ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var now1 = DateTime.Now;
        var lastProgressUpdate = now1;
        ProgressUpdateCallback? wrappedProgress = null;
        if (progress != null)
            wrappedProgress = p =>
            {
                var now2 = DateTime.Now;
                var timeSpan = now2 - lastProgressUpdate;
                var bitRate = 8.0 * p.BytesRead / timeSpan.TotalSeconds;
                progress(new ProgressUpdateStatus(p.BytesRead, p.TotalBytes, bitRate));
                lastProgressUpdate = now2;
            };
        var downloadSummary = DownloadCore(uri, outputStream, wrappedProgress, cancellationToken);
        downloadSummary.DownloadTime = DateTime.Now - now1;
        downloadSummary.BitRate = 8.0 * downloadSummary.DownloadedSize / downloadSummary.DownloadTime.TotalSeconds;
        return downloadSummary;
    }
}