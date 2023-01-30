using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

/// <summary>
/// Base implementation for an <see cref="IDownloadProvider"/>.
/// </summary>
public abstract class DownloadProviderBase : DisposableObject, IDownloadProvider {
    
    private readonly HashSet<DownloadSource> _supportedSources;

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// Initializes an <see cref="IDownloadProvider"/> instance.
    /// </summary>
    /// <param name="name">The name of the concrete instance.</param>
    /// <param name="supportedSources">The supported download locations by this instance.</param>
    protected DownloadProviderBase(string name, DownloadSource supportedSources) : this(name, new []{supportedSources})
    {
    }

    /// <summary>
    /// Initializes an <see cref="IDownloadProvider"/> instance.
    /// </summary>
    /// <param name="name">The name of the concrete instance.</param>
    /// <param name="supportedSources">The supported download locations by this instance.</param>
    protected DownloadProviderBase(string name, IEnumerable<DownloadSource> supportedSources)
    {
        Requires.NotNullOrEmpty(name, nameof(name));
        Name = name;
        _supportedSources = new HashSet<DownloadSource>(supportedSources);
    }

    /// <inheritdoc/>
    public bool IsSupported(DownloadSource source)
    {
        return _supportedSources.Contains(source);
    }

    /// <inheritdoc/>
    public Task<DownloadSummary> DownloadAsync(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
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
    /// <returns>A summary of the download operation.</returns>
    protected abstract Task<DownloadSummary> DownloadAsyncCore(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken);

    private async Task<DownloadSummary> DownloadWithBitRate(
        Uri uri,
        Stream outputStream,
        ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var start = DateTime.Now;
        var lastProgressUpdate = start;
        ProgressUpdateCallback? wrappedProgress = null;
        if (progress != null)
            wrappedProgress = p =>
            {
                var now = DateTime.Now;
                var timeSpan = now - lastProgressUpdate;
                var bitRate = 8.0 * p.BytesRead / timeSpan.TotalSeconds;
                progress(new ProgressUpdateStatus(p.BytesRead, p.TotalBytes, bitRate));
                lastProgressUpdate = now;
            };
        var downloadSummary = await DownloadAsyncCore(uri, outputStream, wrappedProgress, cancellationToken).ConfigureAwait(false);
        downloadSummary.DownloadTime = DateTime.Now - start;
        downloadSummary.BitRate = 8.0 * downloadSummary.DownloadedSize / downloadSummary.DownloadTime.TotalSeconds;
        return downloadSummary;
    }
}