﻿using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

internal class FileDownloader : DownloadProviderBase
{
    private readonly IServiceProvider _serviceProvider;

    public FileDownloader(IServiceProvider serviceProvider) : base("File", DownloadSource.File)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<DownloadSummary> DownloadAsyncCore(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        if (!uri.IsFile && !uri.IsUnc)
            throw new ArgumentException("Expected file or UNC path", nameof(uri));
        return new DownloadSummary
        {
            DownloadedSize = await CopyFileToStreamAsync(uri.LocalPath, outputStream, progress, cancellationToken).ConfigureAwait(false)
        };
    }

    private Task<long> CopyFileToStreamAsync(string filePath, Stream outStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();
        if (!fileSystem.File.Exists(filePath))
            throw new FileNotFoundException(nameof(filePath));
        using var fileStream = fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);
        return StreamUtilities.CopyStreamWithProgressAsync(fileStream, outStream, progress, cancellationToken);
    }
}