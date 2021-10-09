using System;
using System.Collections.Generic;
using System.Text;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Aggregated exception which holds all <see cref="DownloadFailureInformation"/> of a file download operation.
/// </summary>
public class DownloadFailureException : Exception
{
    /// <summary>
    /// All failures during a file download operation.
    /// </summary>
    public IEnumerable<DownloadFailureInformation> DownloadFailures { get; }

    /// <summary>
    /// Detailed failure message with all occurred failures.
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
                stringBuilder.Append(downloadFailure.Engine);
                stringBuilder.Append(" download failed: ");
                stringBuilder.Append(downloadFailure.Exception.Message);
            }
            return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// Creates a new <see cref="DownloadFailureException"/> exception.
    /// </summary>
    /// <param name="downloadFailures">Failures which occurred during a file download.</param>
    public DownloadFailureException(IEnumerable<DownloadFailureInformation> downloadFailures)
    {
        DownloadFailures = downloadFailures;
    }
}