using System.IO;

namespace AnakinRaW.CommonUtilities.DownloadManager.Verification;

/// <summary>
/// Service to verify a file against a pre-defined constraints.
/// </summary>
public interface IVerifier
{
    /// <summary>
    /// Verifies the given <paramref name="file"/> against a <see cref="IVerificationContext"/>.
    /// </summary>
    /// <param name="file">The file to verify.</param>
    /// <param name="verificationContext">The context to verify <paramref name="file"/> against.</param>
    /// <returns>Status information of the verification.</returns>
    public VerificationResult Verify(Stream file, IVerificationContext verificationContext);
}


/// <summary>
/// Service to verify a file against a pre-defined constraints.
/// </summary>
/// <typeparam name="T">The type which holds the information for this verifier to perform the necessary checks.</typeparam>
public interface IVerifier<in T> : IVerifier
{
    /// <summary>
    /// Verifies the given <paramref name="file"/> against a <see cref="IVerificationContext"/>.
    /// </summary>
    /// <param name="file">The file to verify.</param>
    /// <param name="verificationContext">The context to verify <paramref name="file"/> against.</param>
    /// <returns>Status information of the verification.</returns>
    public VerificationResult Verify(Stream file, IVerificationContext<T> verificationContext);
}