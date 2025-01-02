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
    /// Adds an <see cref="IDownloadProvider"/> to this instance.
    /// </summary>
    /// <param name="provider">The provider to add.</param>
    /// <exception cref="InvalidOperationException">if a provider with the same name already exists.</exception>
    void AddDownloadProvider(IDownloadProvider provider);

    /// <summary>
    /// Downloads a file asynchronously.
    /// </summary>
    /// <param name="uri">The source location of the file.</param>
    /// <param name="outputStream">The output stream where to download the file to.</param>
    /// <param name="progress">Optional progress callback.</param>
    /// <param name="validator">The validator instance to validate the downloaded file, or <see langword="null"/> if no validation shall be performed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task object producing a summary of the download operation.</returns>
    Task<DownloadResult> DownloadAsync(Uri uri, Stream outputStream, DownloadUpdateCallback? progress = null,
        IDownloadValidator? validator = null, CancellationToken cancellationToken = default);
}