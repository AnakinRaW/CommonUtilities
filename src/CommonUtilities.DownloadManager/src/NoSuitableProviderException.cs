using System;
using Sklavenwalker.CommonUtilities.DownloadManager.Providers;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Get's thrown if there could be no <see cref="IDownloadProvider"/> found for a download operation.
/// </summary>
public class NoSuitableProviderException : InvalidOperationException
{
    /// <summary>
    /// Creates the exception
    /// </summary>
    /// <param name="message">The message of the exception</param>
    public NoSuitableProviderException(string message)
        : base(message)
    {
    }
}