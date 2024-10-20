using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

internal class FileDownloader(IServiceProvider serviceProvider) : DownloadProviderBase("File", DownloadKind.File)
{
    protected override async Task<DownloadResult> DownloadAsyncCore(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        if (uri is { IsFile: false, IsUnc: false })
            throw new ArgumentException("Expected file or UNC path", nameof(uri));
        return new DownloadResult
        {
            Uri = uri.LocalPath,
            DownloadedSize = await CopyFileToStreamAsync(uri.LocalPath, outputStream, progress, cancellationToken).ConfigureAwait(false)
        };
    }

    private async Task<long> CopyFileToStreamAsync(string filePath, Stream outStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        if (!fileSystem.File.Exists(filePath))
            throw new FileNotFoundException(nameof(filePath));
#if NETSTANDARD2_1 || NET
        await using var fileStream = fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);
#else
        using var fileStream = fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);
#endif

        return await StreamUtilities.CopyStreamWithProgressAsync(fileStream, outStream, progress, cancellationToken);
    }
}