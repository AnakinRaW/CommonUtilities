namespace AnakinRaW.CommonUtilities.Verification.Empty;

/// <summary>
/// A verification context that shall always be verified successful.
/// </summary>
public readonly struct EmptyVerificationContext : IVerificationContext
{
    bool IVerificationContext.Verify()
    {
        return true;
    }
}