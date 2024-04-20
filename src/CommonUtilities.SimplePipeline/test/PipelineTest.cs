using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class PipelineTest
{
    [Fact]
    public async Task Test_Prepare()
    {
        var sp = new Mock<IServiceProvider>().Object;
        var pipeline = new Mock<Pipeline>(sp)
        {
            CallBase = true
        };

        await pipeline.Object.PrepareAsync();
        await pipeline.Object.PrepareAsync();

        pipeline.Protected().Verify<Task<bool>>("PrepareCoreAsync", Times.Exactly(1));
    }

    [Fact]
    public async Task Test_Run()
    {
        var sp = new Mock<IServiceProvider>().Object;
        var pipeline = new Mock<Pipeline>(sp)
        {
            CallBase = true
        };

        pipeline.Protected().Setup<Task<bool>>("PrepareCoreAsync").Returns(Task.FromResult(true));
        
        await pipeline.Object.RunAsync();
        await pipeline.Object.RunAsync();

        pipeline.Protected().Verify<Task<bool>>("PrepareCoreAsync", Times.Exactly(1));
        pipeline.Protected().Verify("RunCoreAsync", Times.Exactly(2), false, (CancellationToken) default);
    }

    [Fact]
    public async Task Test_Prepare_Run()
    {
        var sp = new Mock<IServiceProvider>().Object;
        var pipeline = new Mock<Pipeline>(sp)
        {
            CallBase = true
        };

        pipeline.Protected().Setup<Task<bool>>("PrepareCoreAsync").Returns(Task.FromResult(true));

        await pipeline.Object.PrepareAsync();
        await pipeline.Object.RunAsync();
        await pipeline.Object.RunAsync();

        pipeline.Protected().Verify<Task<bool>>("PrepareCoreAsync", Times.Exactly(1));
        pipeline.Protected().Verify("RunCoreAsync", Times.Exactly(2), false, (CancellationToken)default);
    }

    [Fact]
    public async Task Test_Run_Cancelled_ThrowsOperationCanceledException()
    {
        var sp = new Mock<IServiceProvider>().Object;
        var pipeline = new Mock<Pipeline>(sp)
        {
            CallBase = true
        };

        pipeline.Protected().Setup<Task<bool>>("PrepareCoreAsync").Returns(Task.FromResult(true));

        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pipeline.Object.RunAsync(cts.Token));
    }

    [Fact]
    public async Task Test_Prepare_Disposed_ThrowsObjectDisposedException()
    {
        var sp = new Mock<IServiceProvider>().Object;
        var pipeline = new Mock<Pipeline>(sp)
        {
            CallBase = true
        };

        pipeline.Object.Dispose();
        pipeline.Object.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async() => await pipeline.Object.PrepareAsync());
    }

    [Fact]
    public async Task Test_Run_Disposed_ThrowsObjectDisposedException()
    {
        var sp = new Mock<IServiceProvider>().Object;
        var pipeline = new Mock<Pipeline>(sp)
        {
            CallBase = true
        };

        await pipeline.Object.PrepareAsync();
        pipeline.Object.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pipeline.Object.RunAsync());
    }
}