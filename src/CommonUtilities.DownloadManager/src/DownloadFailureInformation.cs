using System;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Contains information about a failed download
/// </summary>
public sealed class DownloadFailureInformation
{
    /// <summary>
    /// The exception of the download failure.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// The provider which caused the failure.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Crates a new <see cref="DownloadFailureInformation"/> instance.
    /// </summary>
    /// <param name="exception">The exception of the download failure.</param>
    /// <param name="provider">The provider which caused the failure.</param>
    public DownloadFailureInformation(Exception exception, string provider)
    {
        Exception = exception;
        Provider = provider;
    }
}