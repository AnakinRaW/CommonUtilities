using System;
using Sklavenwalker.CommonUtilities.DownloadManager.Engines;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Get's thrown if there could be no <see cref="IDownloadEngine"/> found for a download operation.
/// </summary>
public class NoSuitableEngineException : InvalidOperationException
{
    /// <summary>
    /// Creates the exception
    /// </summary>
    /// <param name="message">The message of the exception</param>
    public NoSuitableEngineException(string message)
        : base(message)
    {
    }
}