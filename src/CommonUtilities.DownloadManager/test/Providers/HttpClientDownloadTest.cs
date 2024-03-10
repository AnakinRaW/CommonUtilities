using System;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;
using Xunit;
using Xunit.Abstractions;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public class HttpClientDownloadTest
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly HttpClientDownloader _provider;

    public HttpClientDownloadTest(ITestOutputHelper outputHelper)
    {
        var sc = new ServiceCollection();
        sc.AddLogging(builder => builder.AddXUnit(outputHelper));
        sc.AddSingleton<IFileSystem>(_fileSystem);
        _provider = new HttpClientDownloader(sc.BuildServiceProvider());
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadNotFound()
    {
        var outStream = new MemoryStream();
        
        await Assert.ThrowsAsync<HttpRequestException>(async () => await _provider.DownloadAsync(
            new Uri("https://example.com/test.txt"), outStream, null,
            CancellationToken.None));
    }

    [Fact]
    public async Task Test_DownloadAsync_Download()
    {
        var outStream = new MemoryStream();
        var result = await _provider.DownloadAsync(
            new Uri("http://speedtest.ftp.otenet.gr/files/test100k.db"),
            outStream, null, CancellationToken.None);
        Assert.True(result.DownloadedSize > 0);
        Assert.Equal(result.DownloadedSize, outStream.Length);
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadCancelled()
    {
        var outStream = new MemoryStream();
        var cts = new CancellationTokenSource();
#if NET
         await cts.CancelAsync();
#else
        cts.Cancel();
#endif

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _provider.DownloadAsync(new Uri("https://example.com/test.txt"), outStream, null, cts.Token));
    }
}