using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public abstract class InternetDownloadTest : DownloadProviderTestBase
{
    protected abstract void AssertRequiredUserAgentMissingException(Exception exception);

    protected override Uri CreateSource(bool exists)
    {
        if (!exists)
            return new Uri("https://example.com/notFound.txt");
        return new Uri(
            "https://raw.githubusercontent.com/AnakinRaW/CommonUtilities/2ab2e6a26872974422459b0605b26222c9e126ca/README.md");
    }

    [Fact]
    public async Task DownloadAsync_WithUserAgent()
    {
        var outStream = new MemoryStream();
        var source = new Uri("https://api.github.com/repos/AnakinRaW/CommonUtilities/releases/latest");

        var options = new DownloadOptions
        {
            UserAgent = "AnakinRaw.DownloadManager.Test"
        };

        var result = await Download(source, outStream, options);
        Assert.True(result.DownloadedSize > 0);
        Assert.Equal(result.DownloadedSize, outStream.Length);
    }

    [Fact]
    public async Task DownloadAsync_RequiredUserAgentNotSet_Throws()
    {
        var outStream = new MemoryStream();
        var source = new Uri("https://api.github.com/repos/AnakinRaW/CommonUtilities/releases/latest");

        var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await Download(source, outStream, null));
        AssertRequiredUserAgentMissingException(exception);
    }
}