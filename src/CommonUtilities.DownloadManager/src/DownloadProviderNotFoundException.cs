using System;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// The exception that is thrown when there is no <see cref="IDownloadProvider"/> found for a download operation .
/// </summary>
public sealed class DownloadProviderNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DownloadProviderNotFoundException class.
    /// </summary>
    /// <param name="message">The message of the exception</param>
    public DownloadProviderNotFoundException(string message)
        : base(message)
    {
    }
}