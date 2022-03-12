#if NET48
using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Sklavenwalker.CommonUtilities.DownloadManager.Providers;
using Xunit;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Test.Providers;

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
    public void TestDownloadNotFound()
    {
        var outStream = new MemoryStream();
        Assert.Throws<WebException>(() =>
            _provider.Download(new Uri("https://example.com/test.txt"), outStream, null, CancellationToken.None));
    }

    [Fact]
    public void TestDownload()
    {
        var outStream = new MemoryStream();
        var result = _provider.Download(
            new Uri("https://www.learningcontainer.com/wp-content/uploads/2020/05/sample-zip-file.zip"),
            outStream, null, CancellationToken.None);
        Assert.True(result.DownloadedSize > 0);
        Assert.Equal(result.DownloadedSize, outStream.Length);
    }

    [Fact]
    public void TestDownloadCancelled()
    {
        var outStream = new MemoryStream();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            _provider.Download(new Uri("https://example.com/test.txt"), outStream, null, cts.Token));
    }
}
#endif