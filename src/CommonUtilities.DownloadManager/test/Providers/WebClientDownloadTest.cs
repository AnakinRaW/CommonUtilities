﻿#if NET48
using System;
using System.IO;
using System.IO.Abstractions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public class WebClientDownloadTest
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly WebClientDownloader _provider;

    public WebClientDownloadTest()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(_fileSystem);
        _provider = new WebClientDownloader(sc.BuildServiceProvider());
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadNotFound()
    {
        var outStream = new MemoryStream();
        await Assert.ThrowsAsync<WebException>(() =>
            _provider.DownloadAsync(new Uri("https://example.com/test.txt"), outStream, null, CancellationToken.None));
    }

    [Fact]
    public async Task Test_DownloadAsync_Download()
    {
        var outStream = new MemoryStream();
        var result = await _provider.DownloadAsync(
            new Uri("https://raw.githubusercontent.com/AnakinRaW/CommonUtilities/2ab2e6a26872974422459b0605b26222c9e126ca/README.md"),
            outStream, null, CancellationToken.None);
        Assert.True(result.DownloadedSize > 0);
        Assert.Equal(result.DownloadedSize, outStream.Length);
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadCancelled()
    {
        var outStream = new MemoryStream();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _provider.DownloadAsync(new Uri("https://example.com/test.txt"), outStream, null, cts.Token));
    }
}
#endif