using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public abstract class DownloadProviderTestBase : CommonTestBase
{
    protected abstract Type ExpectedSourceNotFoundExceptionType { get; }

    protected abstract IDownloadProvider CreateProvider();

    protected abstract Uri CreateSource(bool exists);

    [Fact]
    public async Task DownloadAsync_SourceNotFound_Throws()
    {
        var source = CreateSource(false);
        await Assert.ThrowsAsync(ExpectedSourceNotFoundExceptionType, async () => await Download(source, new MemoryStream()));
    }

    [Fact]
    public async Task DownloadAsync()
    {
        var outStream = new MemoryStream();
        var source = CreateSource(true);

        var result = await Download(source, outStream);
        Assert.True(result.DownloadedSize > 0);
        Assert.Equal(result.DownloadedSize, outStream.Length);
    }

    [Fact]
    public async Task DownloadAsync_DownloadCancelled_Throws()
    {
        var outStream = new MemoryStream();
        var source = CreateSource(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await Download(source, outStream, new CancellationToken(true)));
    }

    protected async Task<DownloadResult> Download(Uri source, Stream outStream, CancellationToken token = default)
    {
        var provider = CreateProvider();

        var callBackFired = false;

        var result = await provider.DownloadAsync(source, outStream, Callback, token);

        Assert.True(callBackFired);

        Assert.Equal(source, result.Uri);
        Assert.NotEqual(0, result.BitRate);
        Assert.NotEqual(TimeSpan.Zero, result.DownloadTime);
        return result;

        void Callback(DownloadUpdate status)
        {
            Task.Delay(100).Wait();
            callBackFired = true;
        }
    }
}