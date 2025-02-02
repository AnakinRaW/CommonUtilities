using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

/// <summary>
/// Base implementation for an <see cref="IDownloadProvider"/>.
/// </summary>
public abstract class DownloadProviderBase : DisposableObject, IDownloadProvider {
    
    private readonly HashSet<DownloadKind> _supportedSources;

    /// <summary>
    /// Returns the service provider the download provider.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadProviderBase"/> class.
    /// </summary>
    /// <param name="name">The name of the concrete instance.</param>
    /// <param name="supportedKinds">The supported download locations by this instance.</param>
    /// <param name="serviceProvider">The service provider.</param>
    protected DownloadProviderBase(string name, DownloadKind supportedKinds, IServiceProvider serviceProvider) 
        : this(name, [supportedKinds], serviceProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadProviderBase"/> class.
    /// </summary>
    /// <param name="name">The name of the concrete instance.</param>
    /// <param name="supportedSources">The supported download locations by this instance.</param>
    /// <param name="serviceProvider">The service provider.</param>
    protected DownloadProviderBase(string name, IEnumerable<DownloadKind> supportedSources, IServiceProvider serviceProvider)
    {
        ThrowHelper.ThrowIfNullOrEmpty(name);
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Name = name;
        _supportedSources = [..supportedSources];
    }

    /// <inheritdoc/>
    public bool IsSupported(DownloadKind kind)
    {
        return _supportedSources.Contains(kind);
    }

    /// <inheritdoc/>
    public Task<DownloadResult> DownloadAsync(Uri uri, Stream outputStream, DownloadUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        return DownloadWithBitRate(uri, outputStream, progress, cancellationToken);
    }

    /// <summary>
    /// Concrete implementation for downloading a file.
    /// </summary>
    /// <remarks>Download time and bit rate is automatically set after this method returns.</remarks>
    /// <param name="uri">The location of the source file.</param>
    /// <param name="outputStream">The output stream.</param>
    /// <param name="progress">Progress with already updated performance data.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the download operation.</returns>
    protected abstract Task<DownloadResult> DownloadAsyncCore(Uri uri, Stream outputStream, DownloadUpdateCallback? progress,
        CancellationToken cancellationToken);

    private async Task<DownloadResult> DownloadWithBitRate(
        Uri uri,
        Stream outputStream,
        DownloadUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var start = DateTime.Now;
        var lastProgressUpdate = start;
        DownloadUpdateCallback? wrappedProgress = null;
        if (progress != null)
            wrappedProgress = p =>
            {
                var now = DateTime.Now;
                var timeSpan = now - lastProgressUpdate;
                var bitRate = 8.0 * p.BytesRead / timeSpan.TotalSeconds;
                progress(new DownloadUpdate(p.BytesRead, p.TotalBytes, bitRate));
                lastProgressUpdate = now;
            };
        var downloadSummary = await DownloadAsyncCore(uri, outputStream, wrappedProgress, cancellationToken).ConfigureAwait(false);
        downloadSummary.DownloadTime = DateTime.Now - start;
        downloadSummary.BitRate = 8.0 * downloadSummary.DownloadedSize / downloadSummary.DownloadTime.TotalSeconds;
        return downloadSummary;
    }
}