using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.DownloadManager.Verification;
using AnakinRaW.CommonUtilities.DownloadManager.Verification.HashVerification;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class DownloadManagerTest
{
    private readonly Mock<IVerificationManager> _verificationManager;
    private readonly DownloadManagerConfiguration _configuration;

    public DownloadManagerTest()
    {
        _verificationManager = new Mock<IVerificationManager>();
        _configuration = DownloadManagerConfiguration.Default;
    }

    private DownloadManager CreateManager(IDownloadManagerConfiguration configuration = null)
    {
        configuration ??= _configuration;
        var sc = new ServiceCollection();
        sc.AddSingleton(_ => _verificationManager.Object);
        sc.AddSingleton(_ => configuration);
        var manager = new DownloadManager(sc.BuildServiceProvider());
        manager.RemoveAllEngines();
        return manager;
    }

    [Fact]
    public void TestAddProviders()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");

        var manager = CreateManager();

        Assert.Empty(manager.Providers);
        manager.AddDownloadProvider(p.Object);
        Assert.Single(manager.Providers);
        Assert.Throws<InvalidOperationException>(() => manager.AddDownloadProvider(p.Object));
    }

    [Fact]
    public async Task TestNoDownloadProvider()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);

        var manager = CreateManager();

        manager.AddDownloadProvider(p.Object);

        var output = new MemoryStream();
        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () =>
            await manager.DownloadAsync(new Uri("http://example.com"), output, null, null, CancellationToken.None));
        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () =>
            await manager.DownloadAsync(new Uri("https://example.com"), output, null, null, CancellationToken.None));
        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () =>
            await manager.DownloadAsync(new Uri("ftp://example.com"), output, null, null, CancellationToken.None));

        manager.RemoveAllEngines();

        p.Setup(x => x.IsSupported(DownloadSource.Internet)).Returns(true);
        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () =>
            await manager.DownloadAsync(new Uri("file://example.txt"), output, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task TestUriNotSupported()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);

        var manager = CreateManager();

        manager.AddDownloadProvider(p.Object);
        var output = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await manager.DownloadAsync(new Uri("sftp://example.com"), output, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task TestDownloadEmpty()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);

        var config = _configuration with { AllowEmptyFileDownload = true };
        var manager = CreateManager(config);

        manager.AddDownloadProvider(p.Object);
        var output = new MemoryStream();
        await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None));

        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadSummary()));

        var summary = await manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None);
        Assert.Equal("A", summary.DownloadProvider);
    }

    [Fact]
    public async Task TestDownload()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);


        var config = _configuration with { AllowEmptyFileDownload = true };
        var manager = CreateManager(config);

        manager.AddDownloadProvider(p.Object);
        var output = new MemoryStream();
        output.WriteByte(1);

        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback(() =>
            {
                output.WriteByte(2);
            })
            .Returns(Task.FromResult(new DownloadSummary()));

        
        await manager.DownloadAsync(new Uri("file://test.txt"), output, null, null, CancellationToken.None);

        var outData = new byte[2];
        output.Seek(0, SeekOrigin.Begin);
        output.Read(outData, 0, (int)output.Length);
        Assert.Equal(new byte[]{1, 2}, outData);
    }

    [Fact]
    public async Task TesProgress()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);

        var manager = CreateManager();

        manager.AddDownloadProvider(p.Object);
        var output = new MemoryStream();

        var called = false;

        void Action(ProgressUpdateStatus status)
        {
            Assert.Equal(1, status.BytesRead);
            Assert.Equal(1, status.TotalBytes);
            Assert.Equal("A", status.DownloadProvider);
            called = true;
        }

        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback<Uri, Stream, ProgressUpdateCallback, CancellationToken>((_, stream, callback, _) =>
            {
                stream.WriteByte(1);
                callback(new ProgressUpdateStatus(1, 1, 0));
            })
            .Returns(Task.FromResult(new DownloadSummary()));

        await manager.DownloadAsync(new Uri("file://test.txt"), output, Action, null, CancellationToken.None);
        Assert.True(called);
    }

    [Fact]
    public async Task TestDownloadFailed()
    {
        var providerA = new Mock<IDownloadProvider>();
        providerA.Setup(x => x.Name).Returns("A");
        providerA.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);

        var config = _configuration with { AllowEmptyFileDownload = true };
        var manager = CreateManager(config);

        manager.AddDownloadProvider(providerA.Object);
        var output = new MemoryStream();
        output.WriteByte(1);

        providerA.Setup(x =>
                x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback(() =>
            {
                output.WriteByte(2);
            })
            .Throws<AccessViolationException>();

        var ex = await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://test.txt"), output, null, null, CancellationToken.None));
        Assert.Equal(2, output.Length);
        var failure = Assert.Single(ex.DownloadFailures);
        Assert.Equal("A", failure.Provider);
        Assert.IsType<AccessViolationException>(failure.Exception);
    }

    [Fact]
    public async Task TestDownloadWithRetry()
    {
        var providerA = new Mock<IDownloadProvider>();
        providerA.Setup(x => x.Name).Returns("A");
        providerA.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);
        var providerB = new Mock<IDownloadProvider>();
        providerB.Setup(x => x.Name).Returns("B");
        providerB.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);

        var config = _configuration with { AllowEmptyFileDownload = true, DownloadRetryDelay = 200};
        var manager = CreateManager(config);

        manager.AddDownloadProvider(providerA.Object);
        manager.AddDownloadProvider(providerB.Object);
        var output = new MemoryStream();
        output.WriteByte(1);

        providerA.Setup(x =>
                x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback(() =>
            {
                output.WriteByte(2);
            })
            .Throws<Exception>();

        providerB.Setup(x =>
                x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback(() =>
            {
                output.WriteByte(3);
                output.WriteByte(4);
            })
            .Returns(Task.FromResult(new DownloadSummary()));

        var summary = await manager.DownloadAsync(new Uri("file://test.txt"), output, null, null, CancellationToken.None);
        Assert.Equal("B", summary.DownloadProvider);

        var outData = new byte[3];
        output.Seek(0, SeekOrigin.Begin);
        output.Read(outData, 0, (int)output.Length);
        Assert.Equal(new byte[] { 1, 3, 4 }, outData);
    }

    [Fact]
    public async Task TestDownloadVerificationEnforce()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);

        var config = _configuration with { AllowEmptyFileDownload = true, VerificationPolicy = VerificationPolicy.Enforce };
        var manager = CreateManager(config);

        manager.AddDownloadProvider(p.Object);
        var output = new MemoryStream();

        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadSummary()));

        var validContext = new HashVerificationContext(new byte[] { }, HashType.None);
        var invalidContext = new HashVerificationContext(new byte[] { }, HashType.MD5);

        await Assert.ThrowsAsync<VerificationFailedException>(() =>
            manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None));
        await Assert.ThrowsAsync<DownloadFailedException>(() =>
            manager.DownloadAsync(new Uri("file://"), output, null, invalidContext, CancellationToken.None));
        _verificationManager.Setup(m => m.Verify(output, It.IsAny<HashVerificationContext>()))
            .Returns(VerificationResult.NotVerified);
        await Assert.ThrowsAsync<DownloadFailedException>( () =>
            manager.DownloadAsync(new Uri("file://"), output, null, validContext, CancellationToken.None));
        _verificationManager.Setup(m => m.Verify(output, It.IsAny<HashVerificationContext>()))
            .Returns(VerificationResult.Success);
        await manager.DownloadAsync(new Uri("file://"), output, null, validContext, CancellationToken.None);
    }

    [Fact]
    public async Task TestDownloadVerificationSkipWhenNoContextOrBroken()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);

        var config = _configuration with { AllowEmptyFileDownload = true, VerificationPolicy = VerificationPolicy.SkipWhenNoContextOrBroken };
        var manager = CreateManager(config);

        manager.AddDownloadProvider(p.Object);
        var output = new MemoryStream();
        await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None));

        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadSummary()));

        var validContext = new HashVerificationContext(new byte[] { }, HashType.None);
        var invalidContext = new HashVerificationContext(new byte[] { }, HashType.MD5);

        await manager.DownloadAsync(new Uri("file://"), output, null, invalidContext, CancellationToken.None);
        _verificationManager.Setup(m => m.Verify(output, It.IsAny<HashVerificationContext>()))
            .Returns(VerificationResult.VerificationContextError);
        await manager.DownloadAsync(new Uri("file://"), output, null, invalidContext, CancellationToken.None);
        _verificationManager.Setup(m => m.Verify(output, It.IsAny<HashVerificationContext>()))
            .Returns(VerificationResult.VerificationFailed);
        await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://"), output, null, validContext, CancellationToken.None));
    }

    [Fact]
    public async Task TestDownloadVerificationOptional()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadSource.File)).Returns(true);

        var config = _configuration with { AllowEmptyFileDownload = true, VerificationPolicy = VerificationPolicy.Optional };
        var manager = CreateManager(config);

        manager.AddDownloadProvider(p.Object);
        var output = new MemoryStream();
        await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None));

        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadSummary()));

        var validContext = new HashVerificationContext(new byte[] { }, HashType.None);
        var invalidContext = new HashVerificationContext(new byte[] { }, HashType.MD5);

        await manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None);
        await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://"), output, null, invalidContext, CancellationToken.None));
    }
}