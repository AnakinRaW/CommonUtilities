using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    /// Evaluates whether a given <see cref="DownloadKind"/> is supported by this provider.
    /// </summary>
    /// <param name="kind">The target <see cref="DownloadKind"/>.</param>
    /// <returns><see langword="true"/> is <paramref name="kind"/> is supported; <see langword="false"/> otherwise.</returns>
    bool IsSupported(DownloadKind kind);

    /// <summary>
    /// Downloads a file.
    /// </summary>
    /// <param name="uri">The source location of the file.</param>
    /// <param name="outputStream">The output stream of the downloaded file.</param>
    /// <param name="progress">A callback reporting the current status of the download.</param>
    /// <param name="cancellationToken">A token to cancel the download operation.</param>
    /// <returns>A summary of the download operation.</returns>
    Task<DownloadResult> DownloadAsync(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken);
}