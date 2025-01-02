using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public class DownloadProviderBaseTest
{
    [Fact]
    public async Task Test_DownloadAsync_DownloadWithBitRate()
    {
        var provMock = new Mock<DownloadProviderBase>("File", DownloadKind.File)
        {
            CallBase = true
        };

        var result = new DownloadResult
        {
            DownloadedSize = 123
        };

        var callBackFired = false;
        void Callback(DownloadUpdate status)
        {
            Assert.Equal(1, status.BytesRead);
            Assert.Equal(2, status.TotalBytes);
            callBackFired = true;
        }

        provMock.Protected()
            .Setup<Task<DownloadResult>>("DownloadAsyncCore", ItExpr.IsAny<Uri>(), ItExpr.IsAny<Stream>(),
                ItExpr.IsAny<DownloadUpdateCallback>(), ItExpr.IsAny<CancellationToken>())
            .Callback((Uri _, Stream _, DownloadUpdateCallback p, CancellationToken _) =>
            {
                Task.Delay(100).Wait();
                p.Invoke(new DownloadUpdate(1, 2, 100));
            })
            .ReturnsAsync(result);

        var prov = provMock.Object;
        var actual = await prov.DownloadAsync(new Uri("file://C:/test.file"), new MemoryStream(), Callback, CancellationToken.None);

        Assert.Same(result, actual);
        Assert.True(actual.DownloadTime != default);
        Assert.True(actual.BitRate != 0);
        Assert.True(callBackFired);
    }
}