using System;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Contains information about a failed download
/// </summary>
public class DownloadFailureInformation
{
    /// <summary>
    /// The exception of the download failure.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// The engine which caused the failure.
    /// </summary>
    public string Engine { get; }

    /// <summary>
    /// Crates a new <see cref="DownloadFailureInformation"/> instance.
    /// </summary>
    /// <param name="exception">The exception of the download failure.</param>
    /// <param name="engine">The engine which caused the failure.</param>
    public DownloadFailureInformation(Exception exception, string engine)
    {
        Exception = exception;
        Engine = engine;
    }
}