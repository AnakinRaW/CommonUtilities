using System.IO;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Verification;

internal interface IVerifier
{
    public VerificationResult Verify(Stream file, VerificationContext verificationContext);
}