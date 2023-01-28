using System;

namespace AnakinRaW.CommonUtilities.DownloadManager.Verification;

/// <summary>
/// Gets thrown if a download verification failed for some reason
/// </summary>
public class VerificationFailedException : Exception
{
    /// <summary>
    /// The result of the failed operation
    /// </summary>
    public VerificationResult Result { get; }

    /// <summary>
    /// Creates a new <see cref="VerificationFailedException"/> exception.
    /// </summary>
    /// <param name="result">The result of the verification operation.</param>
    /// <param name="message">The message of the failure.</param>
    public VerificationFailedException(VerificationResult result, string message) : base(message)
    {
        Result = result;
        HResult = -2146869244;
    }
}