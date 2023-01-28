using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

internal class FileDownloader : DownloadProviderBase
{
    private readonly IServiceProvider _serviceProvider;

    public FileDownloader(IServiceProvider serviceProvider) : base("File", DownloadSource.File)
    {
        _serviceProvider = serviceProvider;
    }

    protected override DownloadSummary DownloadCore(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        if (!uri.IsFile && !uri.IsUnc)
            throw new ArgumentException("Expected file or UNC path", nameof(uri));
        return new DownloadSummary
        {
            DownloadedSize = CopyFileToStream(uri.LocalPath, outputStream, progress, cancellationToken)
        };
    }

    private long CopyFileToStream(string filePath, Stream outStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();
        if (!fileSystem.File.Exists(filePath))
            throw new FileNotFoundException(nameof(filePath));
        using var fileStream = fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);
        return StreamUtilities.CopyStreamWithProgress(fileStream, outStream, progress, cancellationToken);
    }
}