using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class DownloadResultTest
{
    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DownloadResult(null!));
    }

    [Fact]
    public void Ctor_InitProperties_Default()
    {
        var uri = new Uri("http://example.com/file.zip");

        var downloadResult = new DownloadResult(uri);

        Assert.Equal(uri, downloadResult.Uri);
        Assert.Equal(string.Empty, downloadResult.DownloadProvider);
        Assert.Equal(0, downloadResult.DownloadedSize);
        Assert.Equal(0.0, downloadResult.BitRate);
        Assert.Equal(TimeSpan.Zero, downloadResult.DownloadTime);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var uri = new Uri("http://example.com/file.zip");
        var downloadResult = new DownloadResult(uri)
        {
            DownloadedSize = 1024,
            BitRate = 256.0,
            DownloadTime = TimeSpan.FromSeconds(4),
            DownloadProvider = "TestProvider"
        };

        Assert.Equal(1024, downloadResult.DownloadedSize);
        Assert.Equal(256.0, downloadResult.BitRate);
        Assert.Equal(TimeSpan.FromSeconds(4), downloadResult.DownloadTime);
        Assert.Equal("TestProvider", downloadResult.DownloadProvider);
    }
}