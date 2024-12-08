using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.DownloadManager.Validation;

/// <summary>
/// A special <see cref="IDownloadManager"/> that always passes the validation.
/// </summary>
public sealed class AlwaysValidDownloadValidator : IDownloadValidator
{
    /// <summary>
    /// Returns a singleton instance of the <see cref="AlwaysValidDownloadValidator"/> class.
    /// </summary>
    public static readonly AlwaysValidDownloadValidator Instance = new();

    private AlwaysValidDownloadValidator()
    {
    }

    /// <inheritdoc />
    public Task<bool> Validate(Stream stream, long downloadedBytes, CancellationToken token = default)
    {
        return Task.FromResult(true);
    }
}