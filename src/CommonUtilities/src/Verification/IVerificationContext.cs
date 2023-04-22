namespace AnakinRaW.CommonUtilities.Verification;

/// <summary>
/// Holds verification information of a downloaded file.
/// </summary>
public interface IVerificationContext
{
    /// <summary>
    /// Checks whether this instance is valid
    /// </summary>
    /// <returns><see langword="true"/> if this instance is valid; <see langword="false"/> otherwise.</returns>
    bool Verify();
}