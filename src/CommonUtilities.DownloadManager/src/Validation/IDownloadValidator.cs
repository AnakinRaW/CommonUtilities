using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.DownloadManager.Validation;

/// <summary>
/// Service to validate downloaded files.
/// </summary>
public interface IDownloadValidator
{
    /// <summary>
    /// Validates the downloaded stream.
    /// </summary>
    /// <remarks>
    /// <paramref name="stream"/>'s position is not assured to be pointing to the beginning of the downloaded file.
    /// </remarks>
    /// <param name="stream">The downloaded file.</param>
    /// <param name="downloadedBytes">The number of bytes downloaded.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns><see langowrd="true"/> if the download is valid; otherwise, <see langowrd="false"/>.</returns>
    Task<bool> Validate(Stream stream, long downloadedBytes, CancellationToken token = default);
}