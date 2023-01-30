#if NET48
using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task TestDownloadNotFound()
    {
        var outStream = new MemoryStream();
        await Assert.ThrowsAsync<WebException>(() =>
            _provider.DownloadAsync(new Uri("https://example.com/test.txt"), outStream, null, CancellationToken.None));
    }

    [Fact]
    public async Task TestDownload()
    {
        var outStream = new MemoryStream();
        var result = await _provider.DownloadAsync(
            new Uri("http://speedtest.ftp.otenet.gr/files/test100k.db"),
            outStream, null, CancellationToken.None);
        Assert.True(result.DownloadedSize > 0);
        Assert.Equal(result.DownloadedSize, outStream.Length);
    }

    [Fact]
    public async Task TestDownloadCancelled()
    {
        var outStream = new MemoryStream();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _provider.DownloadAsync(new Uri("https://example.com/test.txt"), outStream, null, cts.Token));
    }
}
#endif