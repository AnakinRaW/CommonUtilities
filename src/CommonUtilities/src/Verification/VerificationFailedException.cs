using System;

namespace AnakinRaW.CommonUtilities.Verification;

/// <summary>
/// Gets thrown the verification of some data failed for some reason
/// </summary>
public class VerificationFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationFailedException"/>.
    /// </summary>
    /// <param name="message">The message of the failure.</param>
    public VerificationFailedException(string message) : base(message)
    {
        HResult = -2146869244;
    }
}