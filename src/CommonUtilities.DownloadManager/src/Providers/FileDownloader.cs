using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

/// <summary>
/// A download provider to download files from the file system.
/// </summary>
public sealed class FileDownloader : DownloadProviderBase
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDownloader"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public FileDownloader(IServiceProvider serviceProvider) : base("File", DownloadKind.File, serviceProvider)
    {
        _fileSystem = ServiceProvider.GetRequiredService<IFileSystem>();
    }

    /// <inheritdoc />
    protected override async Task<DownloadResult> DownloadAsyncCore(Uri uri, Stream outputStream, DownloadUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        if (uri is { IsFile: false, IsUnc: false })
            throw new ArgumentException("Expected file or UNC path", nameof(uri));
        return new DownloadResult(uri)
        {
            DownloadedSize = await CopyFileToStreamAsync(uri.LocalPath, outputStream, progress, cancellationToken).ConfigureAwait(false)
        };
    }

    private async Task<long> CopyFileToStreamAsync(string filePath, Stream outStream, DownloadUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
       if (!_fileSystem.File.Exists(filePath))
            throw new FileNotFoundException(nameof(filePath));
#if NETSTANDARD2_1 || NET
        await using var fileStream = _fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);
#else
        using var fileStream = _fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);
#endif

        return await StreamUtilities.CopyStreamWithProgressAsync(fileStream, outStream, progress, cancellationToken);
    }
}