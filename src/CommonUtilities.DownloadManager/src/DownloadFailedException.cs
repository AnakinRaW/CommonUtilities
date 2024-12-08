using System;
using System.Collections.Generic;
using System.Text;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Aggregated exception which holds all <see cref="DownloadFailureInformation"/> of a file download operation.
/// </summary>
public sealed class DownloadFailedException : Exception
{
    /// <summary>
    /// Gets all failures during a file download operation.
    /// </summary>
    public IEnumerable<DownloadFailureInformation> DownloadFailures { get; }

    /// <summary>
    /// Gets a detailed failure message with all occurred failures.
    /// </summary>
    public override string Message
    {
        get
        {
            var stringBuilder = new StringBuilder();
            foreach (var downloadFailure in DownloadFailures)
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(". ");
                stringBuilder.Append(downloadFailure.Provider);
                stringBuilder.Append(" download failed: ");
                stringBuilder.Append(downloadFailure.Exception.Message);
            }
            return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadFailedException"/> class from the specified download failures.
    /// </summary>
    /// <param name="downloadFailures">The failures which occurred during a file download.</param>
    public DownloadFailedException(IEnumerable<DownloadFailureInformation> downloadFailures)
    {
        DownloadFailures = downloadFailures;
    }
}