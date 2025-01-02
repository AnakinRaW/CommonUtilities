using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class DownloadManagerIntegrationTest
{
    [Fact]
    public async Task Test_DownloadAsync_NoProviderFound()
    {
        var fs = new MockFileSystem();
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(fs);

        var serviceProvider = sc.BuildServiceProvider();

        var manager = new DownloadManager(serviceProvider);

        manager.RemoveAllEngines();

        var file = fs.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () => await manager.DownloadAsync(
            new Uri("http://example.com/test.txt"), file, ProgressMethod, null, CancellationToken.None));

        Assert.False(progressTriggered);

        void ProgressMethod(DownloadUpdate status)
        {
            progressTriggered = true;
        }
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadWithHttpClient()
    {
        var fs = new MockFileSystem();
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(fs);

        var serviceProvider = sc.BuildServiceProvider();

        var manager = new DownloadManager(serviceProvider);
        
        manager.RemoveAllEngines();
        manager.AddDownloadProvider(new HttpClientDownloader(serviceProvider));

        var file = fs.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        var summary = await manager.DownloadAsync(new Uri("https://raw.githubusercontent.com/BitDoctor/speed-test-file/master/5mb.txt"), file, ProgressMethod,
            null, CancellationToken.None);

        Assert.Equal(5 * 1024 * 1024, summary.DownloadedSize);
        Assert.Equal(5 * 1024 * 1024, file.Length);
        Assert.True(progressTriggered);

        void ProgressMethod(DownloadUpdate status)
        {
            progressTriggered = true;
        }
    }
    
    [Fact]
    public async Task Test_DownloadAsync_DownloadWithHttpClient_NotFound()
    {
        var fs = new MockFileSystem();
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(fs);

        var serviceProvider = sc.BuildServiceProvider();

        var manager = new DownloadManager(serviceProvider);

        manager.RemoveAllEngines();
        manager.AddDownloadProvider(new HttpClientDownloader(serviceProvider));

        var file = fs.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        await Assert.ThrowsAsync<DownloadFailedException>(async () => await manager.DownloadAsync(
            new Uri("http://example.com/test.txt"), file, ProgressMethod, null, CancellationToken.None));

        Assert.False(progressTriggered);

        void ProgressMethod(DownloadUpdate status)
        {
            progressTriggered = true;
        }
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadWithLocalFile()
    {
        var fs = new MockFileSystem();

        byte[] fileByteData = [1, 2, 3];
        fs.Initialize().WithFile("/test.txt").Which(a => a.HasBytesContent(fileByteData));

        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(fs);

        var serviceProvider = sc.BuildServiceProvider();

        var manager = new DownloadManager(serviceProvider);

        manager.RemoveAllEngines();
        manager.AddDownloadProvider(new FileDownloader(serviceProvider));
        var file = fs.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        var summary = await manager.DownloadAsync(new Uri("file:///test.txt"), file, ProgressMethod,
            null, CancellationToken.None);

        Assert.Equal(3, summary.DownloadedSize);
        Assert.Equal(3, file.Length);
        Assert.True(progressTriggered);

#if NET
        await file.DisposeAsync();
#else
        file.Dispose();
#endif

        Assert.Equal(fileByteData, fs.File.ReadAllBytes(summary.Uri));

        void ProgressMethod(DownloadUpdate status)
        {
            progressTriggered = true;
        }
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadWithLocalFile_NotFound()
    {
        var fs = new MockFileSystem();
        
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(fs);

        var serviceProvider = sc.BuildServiceProvider();

        var manager = new DownloadManager(serviceProvider);

        manager.RemoveAllEngines();
        manager.AddDownloadProvider(new FileDownloader(serviceProvider));

        var file = fs.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        await Assert.ThrowsAsync<DownloadFailedException>(async () => await manager.DownloadAsync(new Uri("file:///test.txt"), file, ProgressMethod,
            null, CancellationToken.None));

        Assert.False(progressTriggered);

        void ProgressMethod(DownloadUpdate status)
        {
            progressTriggered = true;
        }
    }


#if NETFRAMEWORK

    [Fact]
    public async Task Test_DownloadAsync_DownloadWithWebClient()
    {
        var fs = new MockFileSystem();
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(fs);

        var serviceProvider = sc.BuildServiceProvider();

        var manager = new DownloadManager(serviceProvider);

        manager.RemoveAllEngines();
        manager.AddDownloadProvider(new WebClientDownloader(serviceProvider));

        var file = fs.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        var summary = await manager.DownloadAsync(new Uri("https://raw.githubusercontent.com/BitDoctor/speed-test-file/master/5mb.txt"), file, ProgressMethod,
            null, CancellationToken.None);

        Assert.Equal(5 * 1024 * 1024, summary.DownloadedSize);
        Assert.Equal(5 * 1024 * 1024, file.Length);
        Assert.True(progressTriggered);

        void ProgressMethod(DownloadUpdate status)
        {
            progressTriggered = true;
        }
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadWithWebClient_NotFound()
    {
        var fs = new MockFileSystem();
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(fs);

        var serviceProvider = sc.BuildServiceProvider();

        var manager = new DownloadManager(serviceProvider);

        manager.RemoveAllEngines();
        manager.AddDownloadProvider(new WebClientDownloader(serviceProvider));

        var file = fs.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        await Assert.ThrowsAsync<DownloadFailedException>(async () => await manager.DownloadAsync(
            new Uri("http://example.com/test.txt"), file, ProgressMethod,
            null, CancellationToken.None));

        Assert.False(progressTriggered);

        void ProgressMethod(DownloadUpdate status)
        {
            progressTriggered = true;
        }
    }
#endif
}