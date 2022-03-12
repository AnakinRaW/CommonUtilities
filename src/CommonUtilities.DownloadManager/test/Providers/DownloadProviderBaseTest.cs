using System;
using System.IO;
using System.Threading;
using Moq;
using Moq.Protected;
using Sklavenwalker.CommonUtilities.DownloadManager.Providers;
using Xunit;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Test.Providers;

public class DownloadProviderBaseTest
{
    [Fact]
    public void TestDownloadWithBitRate()
    {
        var provMock = new Mock<DownloadProviderBase>("File", DownloadSource.File)
        {
            CallBase = true
        };

        var result = new DownloadSummary
        {
            DownloadedSize = 123
        };

        var callBackFired = false;
        void Callback(ProgressUpdateStatus status)
        {
            Assert.Equal(1, status.BytesRead);
            Assert.Equal(2, status.TotalBytes);
            callBackFired = true;
        }

        provMock.Protected()
            .Setup<DownloadSummary>("DownloadCore", ItExpr.IsAny<Uri>(), ItExpr.IsAny<Stream>(),
                ItExpr.IsAny<ProgressUpdateCallback>(), ItExpr.IsAny<CancellationToken>())
            .Callback((Uri _, Stream _, ProgressUpdateCallback p, CancellationToken _) =>
            {
                p.Invoke(new ProgressUpdateStatus(1, 2, 100));
            })
            .Returns(result);

        var prov = provMock.Object;
        var actual = prov.Download(new Uri("file://C:/test.file"), new MemoryStream(), Callback, CancellationToken.None);

        Assert.Same(result, actual);
        Assert.True(actual.DownloadTime != default);
        Assert.True(actual.BitRate != 0);
        Assert.True(callBackFired);
    }
}