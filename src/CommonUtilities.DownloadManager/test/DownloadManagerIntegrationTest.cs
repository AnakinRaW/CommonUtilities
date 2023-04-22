using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Verification;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class DownloadManagerIntegrationTest
{
    private readonly ITestOutputHelper _helper;

    public DownloadManagerIntegrationTest(ITestOutputHelper helper)
    {
        _helper = helper;
    }

    [Fact]
    public async Task TestDownload()
    {
        var fs = new MockFileSystem();
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(fs);
        sc.AddSingleton<IVerificationManager>(sp => new VerificationManager(sp));
        var manager = new DownloadManager(sc.BuildServiceProvider());

        var file = fs.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        var summary = await manager.DownloadAsync(new Uri("http://speedtest.ftp.otenet.gr/files/test10Mb.db"), file, ProgressMethod,
            null, CancellationToken.None);

        Assert.Equal(10 * 1024 * 1024, summary.DownloadedSize);
        Assert.Equal(10 * 1024 * 1024, file.Length);
        Assert.True(progressTriggered);

        void ProgressMethod(ProgressUpdateStatus status)
        {
            progressTriggered = true;
        }
    }
}