﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sklavenwalker.CommonUtilities.DownloadManager.Engines;
using Sklavenwalker.CommonUtilities.DownloadManager.Verification;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Service to download files.
/// </summary>
public interface IDownloadManager
{
    /// <summary>
    /// Name collection of supported <see cref="IDownloadEngine"/>.
    /// </summary>
    IEnumerable<string> Engines { get; }

    /// <summary>
    /// Adds an <see cref="IDownloadEngine"/> to this instance.
    /// </summary>
    /// <param name="engine"></param>
    void AddDownloadEngine(IDownloadEngine engine);

    /// <summary>
    /// Downloads a file asynchronously.
    /// </summary>
    /// <param name="uri">The source location of the file.</param>
    /// <param name="outputStream">The output stream where to download the file to.</param>
    /// <param name="progress">Progress callback</param>
    /// <param name="verificationContext">The <see cref="VerificationContext"/> of the downloaded file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns></returns>
    Task<DownloadSummary> DownloadAsync(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        VerificationContext? verificationContext = null, CancellationToken cancellationToken = default);
}