using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class DownloadManagerTest
{
    private readonly Mock<IDownloadManagerConfigurationProvider> _configProvider;

    private readonly IServiceProvider _serviceProvider;

    public DownloadManagerTest()
    {
        _configProvider = new Mock<IDownloadManagerConfigurationProvider>();
        var sc = new ServiceCollection();
        sc.AddSingleton(_ => _configProvider.Object);

        _serviceProvider = sc.BuildServiceProvider();
    }

    protected DownloadManager CreateDownloadManager()
    {
        var manager = new DownloadManager(_serviceProvider);
        manager.RemoveAllEngines();
        return manager;
    }

    [Fact]
    public void Test_AddDownloadProvider()
    {
        var manager = CreateDownloadManager();
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        Assert.Empty(manager.Providers);
        manager.AddDownloadProvider(p.Object);
        Assert.Single(manager.Providers);
        Assert.Throws<InvalidOperationException>(() => manager.AddDownloadProvider(p.Object));
    }

    [Fact]
    public async Task Test_DownloadAsync_NoDownloadProvider_ThrowsDownloadProviderNotFoundException()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);

        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);

        var output = new MemoryStream();
        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () =>
            await manager.DownloadAsync(new Uri("http://example.com"), output, null, null, CancellationToken.None));
        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () =>
            await manager.DownloadAsync(new Uri("https://example.com"), output, null, null, CancellationToken.None));
        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () =>
            await manager.DownloadAsync(new Uri("ftp://example.com"), output, null, null, CancellationToken.None));

        manager.RemoveAllEngines();

        p.Setup(x => x.IsSupported(DownloadKind.Internet)).Returns(true);
        await Assert.ThrowsAsync<DownloadProviderNotFoundException>(async () =>
            await manager.DownloadAsync(new Uri("file://example.txt"), output, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task Test_DownloadAsync_UriNotSupported_ThrowsArgumentException()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);

        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);
        var output = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await manager.DownloadAsync(new Uri("sftp://example.com"), output, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadEmpty_ThrowsDownloadFailedException()
    {
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);

        var manager = CreateDownloadManager();

        manager.AddDownloadProvider(p.Object);
        var output = new MemoryStream();
        await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None));

        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadResult()));
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadEmptyAllowed()
    {
        var output = new MemoryStream();
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadResult()));

        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(() => DownloadManagerConfiguration.Default with { AllowEmptyFileDownload = true });

        var manager = CreateDownloadManager();

        manager.AddDownloadProvider(p.Object);

        var result = await manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None);
        Assert.Equal("A", result.DownloadProvider);
    }

    [Fact]
    public async Task Test_DownloadAsync_Download()
    {
        var output = new MemoryStream();
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback(() =>
            {
                output.WriteByte(2);
            })
            .Returns(Task.FromResult(new DownloadResult()));

        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(DownloadManagerConfiguration.Default with { AllowEmptyFileDownload = true });


        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);
       
        // Pretend there was already some data in the stream
        output.WriteByte(1);

        await manager.DownloadAsync(new Uri("file://test.txt"), output, null, null, CancellationToken.None);

        var outData = new byte[2];
        output.Seek(0, SeekOrigin.Begin);
        _ = await output.ReadAsync(outData, 0, (int)output.Length);
        Assert.Equal([1, 2], outData);
    }

    [Fact]
    public async Task Test_DownloadAsync_Progress()
    {
        var output = new MemoryStream();
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback<Uri, Stream, ProgressUpdateCallback, CancellationToken>((_, stream, callback, _) =>
            {
                stream.WriteByte(1);
                callback(new ProgressUpdateStatus(1, 1, 0));
            })
            .Returns(Task.FromResult(new DownloadResult()));

        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);
        

        var called = false;

        void Action(ProgressUpdateStatus status)
        {
            Assert.Equal(1, status.BytesRead);
            Assert.Equal(1, status.TotalBytes);
            Assert.Equal("A", status.DownloadProvider);
            called = true;
        }

        await manager.DownloadAsync(new Uri("file://test.txt"), output, Action, null, CancellationToken.None);
        Assert.True(called);
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadFailed_Throws_DownloadFailedException()
    {
        var output = new MemoryStream();
        var providerA = new Mock<IDownloadProvider>();
        providerA.Setup(x => x.Name).Returns("A");
        providerA.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        providerA.Setup(x =>
                x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback(() =>
            {
                output.WriteByte(2);
            })
            .Throws<AccessViolationException>();

        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(DownloadManagerConfiguration.Default with { AllowEmptyFileDownload = true });

        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(providerA.Object);
       
        output.WriteByte(1);


        var ex = await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://test.txt"), output, null, null, CancellationToken.None));
        Assert.Equal(2, output.Length);
        var failure = Assert.Single(ex.DownloadFailures);
        Assert.Equal("A", failure.Provider);
        Assert.IsType<AccessViolationException>(failure.Exception);
    }

    [Fact]
    public async Task Test_DownloadAsync_DownloadWithRetry()
    {
        var output = new MemoryStream();
        var providerA = new Mock<IDownloadProvider>();
        providerA.Setup(x => x.Name).Returns("A");
        providerA.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        providerA.Setup(x =>
                x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback(() =>
            {
                output.WriteByte(2);
            })
            .Throws<Exception>();

        var providerB = new Mock<IDownloadProvider>();
        providerB.Setup(x => x.Name).Returns("B");
        providerB.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        providerB.Setup(x =>
                x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback(() =>
            {
                output.WriteByte(3);
                output.WriteByte(4);
            })
            .Returns(Task.FromResult(new DownloadResult()));

        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(DownloadManagerConfiguration.Default with
            {
                AllowEmptyFileDownload = true,
                DownloadRetryDelay = 200
            });

        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(providerA.Object);
        manager.AddDownloadProvider(providerB.Object);

       
        output.WriteByte(1);

        var result = await manager.DownloadAsync(new Uri("file://test.txt"), output, null, null, CancellationToken.None);
        Assert.Equal("B", result.DownloadProvider);

        var outData = new byte[3];
        output.Seek(0, SeekOrigin.Begin);
        _ = await output.ReadAsync(outData, 0, (int)output.Length);
        Assert.Equal([1, 3, 4], outData);
    }


    [Fact]
    public async Task Test_DownloadAsync_NoValidator()
    {
        var output = new MemoryStream();
        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(DownloadManagerConfiguration.Default with
            {
                AllowEmptyFileDownload = true,
            });

        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadResult()));

        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);

        await manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None);
    }

    [Fact]
    public async Task Test_DownloadAsync_ValidationSkip()
    {
        var output = new MemoryStream();
        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(DownloadManagerConfiguration.Default with
            {
                AllowEmptyFileDownload = true,
                ValidationPolicy = ValidationPolicy.NoValidation
            });

        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadResult()));


        var validator = new Mock<IDownloadValidator>();
        validator.Setup(v => v.Validate(output, It.IsAny<long>(), CancellationToken.None))
            .Throws<NotSupportedException>();

        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);

        await manager.DownloadAsync(new Uri("file://"), output, null, validator.Object, CancellationToken.None);
    }

    [Fact]
    public async Task Test_DownloadAsync_NoValidatorPresentWhenRequired_ThrowsNotSupportedException()
    {
        var output = new MemoryStream();
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadResult()));

        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(DownloadManagerConfiguration.Default with
            {
                AllowEmptyFileDownload = true,
                ValidationPolicy = ValidationPolicy.Required
            });
        
        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await manager.DownloadAsync(new Uri("file://"), output, null, null, CancellationToken.None));
    }


    [Fact]
    public async Task Test_DownloadAsync_InvalidDownload_ThrowsDownloadFailedException()
    {
        var output = new MemoryStream();
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadResult()));

        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(DownloadManagerConfiguration.Default with
            {
                AllowEmptyFileDownload = true,
                ValidationPolicy = ValidationPolicy.Required
            });

        var validator = new Mock<IDownloadValidator>();
        validator.Setup(v => v.Validate(output, It.IsAny<long>(), CancellationToken.None))
            .Returns(Task.FromResult(false));


        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);

        await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://"), output, null, validator.Object, CancellationToken.None));
    }

    [Fact]
    public async Task Test_DownloadAsync_ValidDownload()
    {
        var output = new MemoryStream();
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Callback(() =>
            {
                Assert.Equal(1, output.Position);
                output.WriteByte(2);
            })
            .Returns(Task.FromResult(new DownloadResult("A", 1, default, default)));

        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(DownloadManagerConfiguration.Default with
            {
                AllowEmptyFileDownload = true,
                ValidationPolicy = ValidationPolicy.Required
            });

        var validator = new Mock<IDownloadValidator>();
        validator.Setup(v => v.Validate(output, It.IsAny<long>(), CancellationToken.None))
            .Callback((Stream o, long dBytes, CancellationToken _) =>
            {
                Assert.Same(o, output);
                Assert.Equal(2, o.Position);
                Assert.Equal(1, dBytes);
            })
            .Returns(Task.FromResult(true));


        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);

        // Pretend there is already data in the output stream.
        output.WriteByte(1);

        await manager.DownloadAsync(new Uri("file://"), output, null, validator.Object, CancellationToken.None);

        validator.Verify(v => v.Validate(output, 1, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Test_DownloadAsync_ValidatorThrows()
    {
        var output = new MemoryStream();
        var p = new Mock<IDownloadProvider>();
        p.Setup(x => x.Name).Returns("A");
        p.Setup(x => x.IsSupported(DownloadKind.File)).Returns(true);
        p.Setup(x => x.DownloadAsync(It.IsAny<Uri>(), output, It.IsAny<ProgressUpdateCallback>(), CancellationToken.None))
            .Returns(Task.FromResult(new DownloadResult()));

        _configProvider.Setup(c => c.GetConfiguration())
            .Returns(DownloadManagerConfiguration.Default with
            {
                AllowEmptyFileDownload = true,
                ValidationPolicy = ValidationPolicy.Optional
            });

        var validator = new Mock<IDownloadValidator>();
        validator.Setup(v => v.Validate(output, It.IsAny<long>(), CancellationToken.None))
            .Throws<NotSupportedException>();


        var manager = CreateDownloadManager();
        manager.AddDownloadProvider(p.Object);

        await Assert.ThrowsAsync<DownloadFailedException>(async () =>
            await manager.DownloadAsync(new Uri("file://"), output, null, validator.Object, CancellationToken.None));
    }
}