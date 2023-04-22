using System.IO;
using System.IO.Abstractions;

namespace AnakinRaW.CommonUtilities.Verification;

/// <summary>
/// Organizes instances of <see cref="IVerifier"/> for file extensions.
/// </summary>
public interface IVerificationManager
{
    /// <summary>
    /// Registers a verifier for the specified context type.
    /// </summary>
    /// <remarks>Note that <see cref="IVerifier{T}"/> is not contravariant which means that's important to register the correct type.</remarks>
    /// <typeparam name="T">The type of the context that the verifier can verify.</typeparam>
    /// <param name="verifier">The verifier instance.</param>
    public void RegisterVerifier<T>(IVerifier<T> verifier) where T : IVerificationContext;

    /// <summary>
    /// Removes a verifier for the specified context type.
    /// </summary>
    /// <param name="verifier">The verifier instance.</param>
    public void RemoveVerifier(IVerifier verifier);


    /// <summary>
    /// Verifies a given stream from start to end against all registered verifiers for that file extension.
    ///
    /// Verification is considered successful if and only if all verifiers succeeded.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="verificationContext">The context information to verify the file against.</param>
    /// <returns>Combined result of all used verifier instances. If none was used the result contains <see cref="VerificationResultStatus.NotVerified"/> is returned.</returns>
    public VerificationResult Verify(Stream data, IVerificationContext verificationContext);

    /// <summary>
    /// Verifies a given file against all registered verifiers for that file extension.
    ///
    /// Verification is considered successful if and only if all verifiers succeeded.
    /// </summary>
    /// <param name="file">The file to verify.</param>
    /// <param name="verificationContext">The context information to verify the file against.</param>
    /// <returns>Combined result of all used verifier instances. If none was used the result contains <see cref="VerificationResultStatus.NotVerified"/> is returned.</returns>
    /// <exception cref="FileNotFoundException"> if <paramref name="file"/> is found.</exception>
    public VerificationResult Verify(IFileInfo file, IVerificationContext verificationContext);
}