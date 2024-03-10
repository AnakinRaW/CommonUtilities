using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

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
    public async Task Test_DownloadAsync()
    {
        const string data = "This is some text.";
        _fileSystem.Initialize().WithFile("test.file").Which(f => f.HasStringContent(data));
        var source = _fileSystem.FileInfo.New("test.file");

        var outStream = new MemoryStream();
        var result = await _provider.DownloadAsync(new Uri($"file://{source.FullName}"), outStream, null, CancellationToken.None);

        Assert.Equal<long>(data.Length, result.DownloadedSize);
        var copyData = Encoding.Default.GetString(outStream.ToArray());
        Assert.Equal(data, copyData);
    }

    [Fact]
    public async Task Test_DownloadAsync_ThrowsFileNotFound()
    {
        var source = _fileSystem.FileInfo.New("test.file");
        var outStream = new MemoryStream();
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _provider.DownloadAsync(new Uri($"file://{source.FullName}"), outStream, null, CancellationToken.None));
    }
}