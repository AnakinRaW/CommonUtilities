using System;

namespace AnakinRaW.CommonUtilities.DownloadManager.Validation;

/// <summary>
/// An exception that is thrown when the validation of a downloaded file failed.
/// </summary>
public class DownloadValidationFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadValidationFailedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DownloadValidationFailedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadValidationFailedException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public DownloadValidationFailedException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}