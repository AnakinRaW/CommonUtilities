using System.IO;

namespace AnakinRaW.CommonUtilities.Verification.Empty;

internal class EmptyContextVerifier : IVerifier<EmptyVerificationContext>
{
    VerificationResult IVerifier<EmptyVerificationContext>.Verify(Stream data, EmptyVerificationContext verificationContext)
    {
        return Verify(data, verificationContext);
    }

    public VerificationResult Verify(Stream data, IVerificationContext verificationContext)
    {
        return VerificationResult.Success;
    }
}