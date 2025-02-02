using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class DownloadManagerTest : CommonTestBase
{
    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
    }

    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DownloadManager(null!, ServiceProvider));
    }

    [Theory]
    [InlineData(InternetClient.HttpClient)]
#if NETFRAMEWORK
    [InlineData(InternetClient.WebClient)]
#endif
    public void Providers_RemoveAllProviders_AddDownloadProvider(InternetClient client)
    {
        var config = new DownloadManagerConfiguration { InternetClient = client };
        var manager = new DownloadManager(config, ServiceProvider);

        Assert.Equal(2, manager.Providers.Count());

        Assert.Contains("File", manager.Providers);

        if (client == InternetClient.HttpClient) 
            Assert.Contains("HttpClient", manager.Providers);

#if NETFRAMEWORK
        if (client == InternetClient.WebClient)
            Assert.Contains("WebClient", manager.Providers);
#endif

        manager.RemoveAllProviders();
        Assert.Empty(manager.Providers);

        manager.AddDownloadProvider(new FileDownloader(ServiceProvider));
        Assert.Contains("File", manager.Providers);
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("test.file")]
    public async Task DownloadAsync_UriRelative_ThrowsArgumentException(string uri)
    {
        var manager = new DownloadManager(ServiceProvider);
        var output = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await manager.DownloadAsync(new Uri(uri, UriKind.RelativeOrAbsolute), output, null, null, CancellationToken.None));
    }

    [Theory]
    [InlineData("sftp://example.com")]
    [InlineData("ftps://example.com")]
    [InlineData("xxx://example.com")]
    public async Task DownloadAsync_UriNotSupported_ThrowsArgumentException(string uri)
    {
        var manager = new DownloadManager(ServiceProvider);
        var output = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await manager.DownloadAsync(new Uri(uri), output, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_NoProviderFound_Throws()
    {
        var manager = new DownloadManager(ServiceProvider);

        manager.RemoveAllProviders();

        var file = FileSystem.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(
            async () => await manager.DownloadAsync(new Uri("http://example.com/test.txt"), file, ProgressMethod, null,
                CancellationToken.None));

        Assert.False(progressTriggered);
        file.Dispose();

        Assert.Empty(FileSystem.File.ReadAllBytes("file.txt")); 
        return;

        void ProgressMethod(DownloadUpdate status)
        {
            progressTriggered = true;
        }
    }

    [Fact]
    public async Task DownloadAsync_WrongProviders_Throws()
    {
        var uri = new Uri("file:///test.txt");
        var provider = new HttpClientDownloader(ServiceProvider);

        var manager = new DownloadManager(ServiceProvider);
        manager.RemoveAllProviders();
        manager.AddDownloadProvider(provider);

        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () =>
            await manager.DownloadAsync(uri, new MemoryStream(), null!, null!, CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_DownloadEmpty_ThrowsDownloadFailedException()
    {
        var manager = new DownloadManager(ServiceProvider);

        var fi = FileSystem.FileInfo.New("empty.txt");
        var _ = fi.Create();
        _.Dispose();

        var output = new MemoryStream();

        var uri = new Uri(fi.FullName);
        var e = await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(uri, output, null, null, CancellationToken.None));

        Assert.Equal($"File download failed: Empty file downloaded on '{uri}'.", e.Message);
    }

    [Fact]
    public async Task DownloadAsync_DownloadEmptyAllowed()
    { 
        var manager = new DownloadManager(new DownloadManagerConfiguration{AllowEmptyFileDownload = true}, ServiceProvider);

        var fi = FileSystem.FileInfo.New("empty.txt");
        var _ = fi.Create();
        _.Dispose();

        var uri = new Uri(fi.FullName);

        var output = new MemoryStream();
        var result = await manager.DownloadAsync(uri, output, null, null, CancellationToken.None);
        Assert.Equal(0, result.DownloadedSize);
    }

    [Fact]
    public async Task DownloadAsync_File5MB_HttpClient()
    {
        var uri = new Uri("https://raw.githubusercontent.com/BitDoctor/speed-test-file/master/5mb.txt");
        var provider = new HttpClientDownloader(ServiceProvider);
        await DownloadAsyncTest(provider, uri, 5 * 1024 * 1024, true);
    }

#if NETFRAMEWORK

    [Fact]
    public async Task DownloadAsync_File5MB_WebClient()
    {
        var uri = new Uri("https://raw.githubusercontent.com/BitDoctor/speed-test-file/master/5mb.txt");
        var provider = new WebClientDownloader(ServiceProvider);
        await DownloadAsyncTest(provider, uri, 5 * 1024 * 1024, true);
    }

#endif

    [Fact]
    public async Task DownloadAsync_LocalFile()
    {
        var uri = new Uri("file:///test.txt");
        var provider = new FileDownloader(ServiceProvider);

        var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        FileSystem.File.WriteAllBytes("test.txt", bytes);

        await DownloadAsyncTest(provider, uri, bytes.Length, true);

        Assert.Equal(bytes, FileSystem.File.ReadAllBytes("file.txt"));
    }

    [Fact]
    public async Task DownloadAsync_HttpClient_SourceNotFound_Throws()
    {
        var uri = new Uri("http://example.com/notFound.txt");
        var provider = new HttpClientDownloader(ServiceProvider);
        await DownloadAsyncTest(provider, uri, 5 * 1024 * 1024, false);
    }

#if NETFRAMEWORK

    [Fact]
    public async Task DownloadAsync_WebClient_SourceNotFound_Throws()
    {
        var uri = new Uri("http://example.com/notFound.txt");
        var provider = new WebClientDownloader(ServiceProvider);
        await DownloadAsyncTest(provider, uri, 5 * 1024 * 1024, false);
    }

#endif

    [Fact]
    public async Task DownloadAsync_LocalFile_SourceNotFound_Throws()
    {
        var uri = new Uri("file:///test.txt");
        var provider = new HttpClientDownloader(ServiceProvider);

        await DownloadAsyncTest(provider, uri, 0, false);

        Assert.Equal([], FileSystem.File.ReadAllBytes("file.txt"));
    }

    [Theory]
    [InlineData(ValidationPolicy.NoValidation)]
    [InlineData(ValidationPolicy.Optional)]
    [InlineData(ValidationPolicy.Required)]
    public async Task DownloadAsync_NoValidatorPresent(ValidationPolicy policy)
    {
        var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        FileSystem.File.WriteAllBytes("test.txt", bytes);

        var output = new MemoryStream();

        var manager = new DownloadManager(new DownloadManagerConfiguration
            {
                ValidationPolicy = policy
            },
            ServiceProvider);

        if (policy == ValidationPolicy.Required)
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await manager.DownloadAsync(new Uri("file:///test.txt"), output, null, null, CancellationToken.None));
        else
        {
            var r = await manager.DownloadAsync(new Uri("file:///test.txt"), output, null, null, CancellationToken.None);
            Assert.Equal(10, r.DownloadedSize);
        }

    }

    [Theory]
    [InlineData(ValidationPolicy.Optional)]
    [InlineData(ValidationPolicy.Required)]
    public async Task DownloadAsync_InvalidDownload_ThrowsDownloadFailedException(ValidationPolicy policy)
    {
        var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        FileSystem.File.WriteAllBytes("test.txt", bytes);

        var output = new MemoryStream();
        var manager = new DownloadManager(new DownloadManagerConfiguration
            {
                ValidationPolicy = policy
            },
            ServiceProvider);

        Span<byte> hash = new byte[16];
        hash.Fill(1);
        var validator = new HashDownloadValidator(hash.ToArray(), HashTypeKey.MD5, ServiceProvider);

        await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri($"file:///{FileSystem.Path.GetFullPath("test.txt")}"), output, null, validator, CancellationToken.None));
    }

    [Theory]
    [InlineData(ValidationPolicy.Optional)]
    [InlineData(ValidationPolicy.Required)]
    public async Task DownloadAsync_ValidDownload(ValidationPolicy policy)
    {
        var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        FileSystem.File.WriteAllBytes("test.txt", bytes);

        var output = new MemoryStream();
        var manager = new DownloadManager(new DownloadManagerConfiguration
            {
                ValidationPolicy = policy
            },
            ServiceProvider);

        var hash = SHA256.Create().ComputeHash(bytes);
        var validator = new HashDownloadValidator(hash, HashTypeKey.SHA256, ServiceProvider);

        await manager.DownloadAsync(new Uri(FileSystem.Path.GetFullPath("test.txt")), output, null, validator, CancellationToken.None);
    }

    [Fact]
    public async Task DownloadAsync_ValidatorThrows()
    {
        var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        FileSystem.File.WriteAllBytes("test.txt", bytes);

        var output = new MemoryStream();
        var manager = new DownloadManager(new DownloadManagerConfiguration
            {
                ValidationPolicy = ValidationPolicy.Required
            },
            ServiceProvider);

        var uri = new Uri(FileSystem.Path.GetFullPath("test.txt"));
        var e = await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(uri, output, null, new ThrowingValidator(),
                CancellationToken.None));

        Assert.Equal($"File download failed: Validation of '{uri}' failed with exception: Test", e.Message);
    }

    [Fact]
    public async Task DownloadAsync_Retry()
    {
        var output = new MemoryStream();
        var manager = new DownloadManager(new DownloadManagerConfiguration { AllowEmptyFileDownload = true }, ServiceProvider);

        manager.RemoveAllProviders();
        var counter = new Counter();

        var provider1 = new DummyFileDownloadProvider("A", counter, ServiceProvider);
        var provider2 = new DummyFileDownloadProvider("B", counter, ServiceProvider);
        manager.AddDownloadProvider(provider1);
        manager.AddDownloadProvider(provider2);

        await manager.DownloadAsync(new Uri($"file:///{FileSystem.Path.GetFullPath("test.txt")}"), output, null, null, CancellationToken.None);

        Assert.Equal(2, counter.Value);
    }


    private async Task DownloadAsyncTest(IDownloadProvider provider, Uri uri, long expectedSize, bool sourceExists)
    {
        var manager = new DownloadManager(ServiceProvider);

        manager.RemoveAllProviders();
        manager.AddDownloadProvider(provider);

        var file = FileSystem.FileStream.New("file.txt", FileMode.Create);

        var progressTriggered = false;

        if (sourceExists)
        {
            var summary = await manager.DownloadAsync(uri, file, ProgressMethod, null, CancellationToken.None);
            Assert.Equal(expectedSize, summary.DownloadedSize);
            Assert.Equal(expectedSize, file.Length);
        }
        else
        {
            var e = await Assert.ThrowsAsync<DownloadFailedException>(async () => await manager.DownloadAsync(
                new Uri("http://example.com/notFound.txt"), file, ProgressMethod, null, CancellationToken.None));
            var failInfo = Assert.Single(e.DownloadFailures);
            Assert.Equal(provider.Name, failInfo.Provider);
        }

          
        Assert.Equal(sourceExists, progressTriggered);

        file.Dispose();
        return;

        void ProgressMethod(DownloadUpdate status)
        {
            progressTriggered = true;
        }
    }

    private class ThrowingValidator : IDownloadValidator
    {
        public Task<bool> Validate(Stream stream, long downloadedBytes, CancellationToken token = default)
        {
            throw new Exception("Test");
        }
    }

    private class DummyFileDownloadProvider(string name, Counter countData, IServiceProvider serviceProvider) : IDownloadProvider
    {
        public string Name => name;

        public bool IsSupported(DownloadKind kind)
        {
            return kind == DownloadKind.File;
        }

        public Task<DownloadResult> DownloadAsync(Uri uri, Stream outputStream, DownloadUpdateCallback progress, CancellationToken cancellationToken)
        {
            countData.Increment();
            var fs = serviceProvider.GetRequiredService<IFileSystem>();

            if (!fs.File.Exists("test.txt"))
            {
                using var _ = fs.File.Create("test.txt");
                throw new InvalidOperationException("Not found");
            }

            return Task.FromResult(new DownloadResult(uri));
        }
    }

    private class Counter
    {
        private int _count;

        public void Increment() => Interlocked.Increment(ref _count);

        public int Value => _count;
    }
}