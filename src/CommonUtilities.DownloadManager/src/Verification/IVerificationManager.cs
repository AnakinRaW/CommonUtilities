using System;
using System.IO;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Verification;

/// <summary>
/// Organizes instances of <see cref="IVerifier"/> for file extensions.
/// </summary>
public interface IVerificationManager
{
    /// <summary>
    /// Registers a given <see cref="IVerifier"/> to a file extensions. 
    /// </summary>
    /// <param name="extension">The file extensions to register, without out pre-leading '.'.</param>
    /// <param name="verifier">The verifier instance.</param>
    /// <remarks>To use the <paramref name="verifier"/> for all file pass in "*"</remarks>
    public void RegisterVerifier(string extension, IVerifier verifier);

    /// <summary>
    /// Removes a verifier for the given file extension.
    /// </summary>
    /// <param name="extension">The file extensions <paramref name="verifier"/> shall be unassigned from.</param>
    /// <param name="verifier">The <see cref="IVerifier"/> to remove.</param>
    public void RemoveVerifier(string extension, IVerifier verifier);

    /// <summary>
    /// Verifies a given file against all registered verifiers for that file extension. 
    /// </summary>
    /// <param name="file">The file to verify.</param>
    /// <param name="verificationContext">The context information to verify the file against.</param>
    /// <returns>Combined result of all used verifier instances. If none was used <see cref="VerificationResult.NotVerified"/> is returned.</returns>
    /// <exception cref="ArgumentException"> if <paramref name="file"/> is not a <see cref="FileStream"/>.</exception>
    /// <remarks>Verification is only considered <see cref="VerificationResult.Success"/>if and only if all verifiers succeeded.</remarks>
    public VerificationResult Verify(Stream file, VerificationContext verificationContext);
}