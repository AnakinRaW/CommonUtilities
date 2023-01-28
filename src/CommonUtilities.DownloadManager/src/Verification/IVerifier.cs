using System.IO;

namespace AnakinRaW.CommonUtilities.DownloadManager.Verification;

/// <summary>
/// Service to verify a file against a pre-defined constraints.
/// </summary>
public interface IVerifier
{
    /// <summary>
    /// Verifies the given <paramref name="file"/> against a <see cref="VerificationContext"/>.
    /// </summary>
    /// <param name="file">The file to verify.</param>
    /// <param name="verificationContext">The context to verify <paramref name="file"/> against.</param>
    /// <returns>Status information of the verification.</returns>
    public VerificationResult Verify(Stream file, VerificationContext verificationContext);
}