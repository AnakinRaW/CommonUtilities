using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Sklavenwalker.CommonUtilities.DownloadManager.Providers;
using Xunit;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Test.Providers;

public class FileDownloadTest
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly FileDownloader _provider;

    public FileDownloadTest()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(_fileSystem);
        _provider = new FileDownloader(sc.BuildServiceProvider());
    }

    [Fact]
    public void TestDownload()
    {
        const string data = "This is some text.";
        _fileSystem.AddFile("test.file", new MockFileData(data));
        var source = _fileSystem.FileInfo.FromFileName("test.file");

        var outStream = new MemoryStream();
        var result = _provider.Download(new Uri($"file://{source.FullName}"), outStream, null, CancellationToken.None);

        Assert.Equal(data.Length, result.DownloadedSize);
        var copyData = Encoding.Default.GetString(outStream.ToArray());
        Assert.Equal(data, copyData);
    }

    [Fact]
    public void TestDownloadFileNotFound()
    {
        var source = _fileSystem.FileInfo.FromFileName("test.file");
        var outStream = new MemoryStream();
        Assert.Throws<FileNotFoundException>(() =>
            _provider.Download(new Uri($"file://{source.FullName}"), outStream, null, CancellationToken.None));
    }
}