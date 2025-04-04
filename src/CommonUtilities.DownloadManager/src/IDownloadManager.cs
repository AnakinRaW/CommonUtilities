using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Service to download files.
/// </summary>
public interface IDownloadManager
{
    /// <summary>
    /// Gets an enumerable collection of the supported download provider names.
    /// </summary>
    IEnumerable<string> Providers { get; }

    /// <summary>
    /// Adds an <see cref="IDownloadProvider"/> to the <see cref="IDownloadManager"/>.
    /// </summary>
    /// <param name="provider">The provider to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="provider"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A provider with the same name already exists.</exception>
    void AddDownloadProvider(IDownloadProvider provider);

    /// <summary>
    /// Downloads a file asynchronously.
    /// </summary>
    /// <param name="uri">The source location of the file.</param>
    /// <param name="outputStream">The output stream where to download the file to.</param>
    /// <param name="progress">Optional progress callback.</param>
    /// <param name="downloadOptions">Additional, optional download options.</param>
    /// <param name="validator">The validator instance to validate the downloaded file, or <see langword="null"/> if no validation shall be performed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task object producing a summary of the download operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> or <paramref name="outputStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException"><paramref name="outputStream"/> is not writable.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> is not an absolute URI.</exception>
    Task<DownloadResult> DownloadAsync(
        Uri uri, 
        Stream outputStream, 
        DownloadUpdateCallback? progress = null,
        DownloadOptions? downloadOptions = null,
        IDownloadValidator? validator = null, 
        CancellationToken cancellationToken = default);
}