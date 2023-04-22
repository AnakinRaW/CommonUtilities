using System.IO;

namespace AnakinRaW.CommonUtilities.Verification;

/// <summary>
/// Service to verify data against a pre-defined constraints.
/// </summary>
public interface IVerifier<T> : IVerifier where T : IVerificationContext
{
    /// <summary>
    /// Verifies the given <paramref name="data"/> against an instance of the verification context <typeparamref name="T"/>.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="verificationContext">The verification context instance to use.</param>
    /// <returns>Status information of the verification.</returns>
    public VerificationResult Verify(Stream data, T verificationContext);
}

/// <summary>
/// Service to verify a file against a pre-defined constraints.
/// </summary>
public interface IVerifier
{
    /// <summary>
    /// Verifies the given <paramref name="data"/> against an instance of the verification context.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="verificationContext">The <see cref="IVerificationContext"/> instance to use.</param>
    /// <returns>Status information of the verification.</returns>
    public VerificationResult Verify(Stream data, IVerificationContext verificationContext);
}

