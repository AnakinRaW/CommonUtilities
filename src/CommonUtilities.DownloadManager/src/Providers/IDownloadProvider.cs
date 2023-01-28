using System;
using System.IO;
using System.Threading;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

/// <summary>
/// Provider which downloads a file in a specific manner.
/// </summary>
public interface IDownloadProvider
{
    /// <summary>
    /// The name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates whether a given <see cref="DownloadSource"/> is supported by this provider.
    /// </summary>
    /// <param name="source">The target <see cref="DownloadSource"/>.</param>
    /// <returns><see langword="true"/> is <paramref name="source"/> is supported; <see langword="false"/> otherwise.</returns>
    bool IsSupported(DownloadSource source);

    /// <summary>
    /// Downloads a file.
    /// </summary>
    /// <param name="uri">The source location of the file.</param>
    /// <param name="outputStream">The output stream of the downloaded file.</param>
    /// <param name="progress">A callback reporting the current status of the download.</param>
    /// <param name="cancellationToken">A token to cancel the download operation.</param>
    /// <returns>A summary of the download operation.</returns>
    DownloadSummary Download(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken);
}