using System;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// A class that contains information about a failed download.
/// </summary>
public sealed class DownloadFailureInformation
{
    /// <summary>
    /// Gets the exception of the download failure.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the provider which caused the failure.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadFailureInformation"/> class of the specified exception and provider name.
    /// </summary>
    /// <param name="exception">The exception of the failed download.</param>
    /// <param name="provider">The provider which caused the failure.</param>
    public DownloadFailureInformation(Exception exception, string provider)
    {
        Exception = exception;
        Provider = provider;
    }
}