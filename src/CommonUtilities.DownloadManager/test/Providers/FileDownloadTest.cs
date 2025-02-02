using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public class FileDownloadTest : DownloadProviderTestBase
{
    private const string Data = "This is some text.";

    protected override Type ExpectedSourceNotFoundExceptionType => typeof(FileNotFoundException);

    protected override IDownloadProvider CreateProvider()
    {
        return new FileDownloader(ServiceProvider);
    }

    protected override Uri CreateSource(bool exists)
    {
        var source = FileSystem.FileInfo.New("test.file");
        if (exists) 
            FileSystem.File.WriteAllText(source.FullName, Data);

        return new Uri($"file://{source.FullName}");
    }

    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FileDownloader(null!));
    }

    [Fact]
    public async Task DownloadAsync_ExpectedData()
    {
        var source = CreateSource(true);
        var outStream = new MemoryStream();
        
        var result = await Download(source, outStream, CancellationToken.None);

        Assert.Equal(Data.Length, result.DownloadedSize);
        var copyData = Encoding.Default.GetString(outStream.ToArray());
        Assert.Equal(Data, copyData);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task DownloadAsync_UncPath()
    {
        var source = new Uri("file://server/test.file");
        Assert.True(source.IsUnc);
        await Assert.ThrowsAsync(ExpectedSourceNotFoundExceptionType, async () => await Download(source, new MemoryStream()));
    }

    [Theory]
    [InlineData("http://example.com/test.txt")]
    [InlineData("https://example.com/test.txt")]
    [InlineData("ftp://example.com/test.txt")]
    [InlineData("xxx://example.com/test.txt")]
    public async Task DownloadAsync_NotAFileSource_Throws(string uri)
    {
        var source = new Uri(uri);
        var outStream = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(async () => await Download(source, outStream, CancellationToken.None));
    }
}