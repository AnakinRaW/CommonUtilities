using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.DownloadManager.Validation;

/// <summary>
/// A <see cref="IDownloadValidator"/> that checks for the correct downloaded byte number.
/// </summary>
public sealed class SizeDownloadValidator : IDownloadValidator
{
    private readonly long _expectedDownloadBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="SizeDownloadValidator"/> class.
    /// </summary>
    /// <param name="expectedDownloadBytes">The expected downloaded byte number.</param>
    public SizeDownloadValidator(long expectedDownloadBytes)
    {
        _expectedDownloadBytes = expectedDownloadBytes;
    }

    /// <inheritdoc />
    public Task<bool> Validate(Stream stream, long downloadedBytes, CancellationToken token = default)
    {
        return Task.FromResult(_expectedDownloadBytes == downloadedBytes);
    }
}