using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public abstract class PipelineTest : CommonTestBase
{
    protected abstract Pipeline CreatePipeline(IList<IStep> steps);

    [Fact]
    public async Task Prepare()
    {
        var s = new TestStep(_ => { }, ServiceProvider);
        var pipeline = CreatePipeline([s]);

        await pipeline.PrepareAsync();
        await pipeline.PrepareAsync();
    }

    [Fact]
    public async Task Dispose()
    {
        var pipeline = CreatePipeline([]);

        pipeline.Dispose();

        Assert.True(pipeline.IsDisposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(pipeline.PrepareAsync);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pipeline.RunAsync());
    }

    [Fact]
    public async Task Run_RunMultipleTimesDoesNotPrepareAgain_StepRunOnlyOnce()
    {
        var counter = 0;
        var s = new TestStep(_ => { counter++; }, ServiceProvider);
        var pipeline = CreatePipeline([s]);

        await pipeline.RunAsync();
        await pipeline.RunAsync();
        
        Assert.Equal(1, counter);
    }

    [Fact]
    public async Task PrepareThenRun()
    {
        var counter = 0;
        var s = new TestStep(_ => { counter++; }, ServiceProvider);
        var pipeline = CreatePipeline([s]);

        await pipeline.PrepareAsync();
        await pipeline.RunAsync();
        Assert.Equal(1, counter);
    }

    [Fact]
    public async Task Run_Cancelled_ThrowsOperationCanceledException()
    {
        var counter = 0;
        var s = new TestStep(_ => { counter++; }, ServiceProvider);
        var pipeline = CreatePipeline([s]);

        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pipeline.RunAsync(cts.Token));
        Assert.Equal(0, counter);
    }

    [Fact]
    public async Task Prepare_Disposed_ThrowsObjectDisposedException()
    {
        var counter = 0;
        var s = new TestStep(_ => { counter++; }, ServiceProvider);
        var pipeline = CreatePipeline([s]);

        pipeline.Dispose();
        pipeline.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(pipeline.PrepareAsync);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pipeline.RunAsync());

        Assert.Equal(0, counter);
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task Run_Disposed_ThrowsObjectDisposedException()
    {
        var counter = 0;
        var s = new TestStep(_ => { counter++; }, ServiceProvider);
        var pipeline = CreatePipeline([s]);

        await pipeline.PrepareAsync();
        pipeline.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pipeline.RunAsync());
        Assert.Equal(0, counter);
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task Cancel()
    {
        var waitToCancel = new TaskCompletionSource<int>();
        var waitUntilCanceled = new ManualResetEvent(false);

        var token = CancellationToken.None;

        var step = new TestStep(ct =>
        {
            token = ct;
            waitToCancel.SetResult(0);
            waitUntilCanceled.WaitOne();

        }, ServiceProvider);

        var pipeline = CreatePipeline([step]);

        await pipeline.PrepareAsync();

        var pipelineTask = pipeline.RunAsync(CancellationToken.None);
        await waitToCancel.Task;
        pipeline.Cancel();
        waitUntilCanceled.Set();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pipelineTask);

        Assert.True(pipeline.PipelineFailed);

        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public async Task Cancel_BeforeRun_HasNoEffect()
    {
        var ran = false;
        var step = new TestStep(_ => ran = true, ServiceProvider);

        var pipeline = CreatePipeline([step]);

        await pipeline.PrepareAsync();
        pipeline.Cancel();

        await pipeline.RunAsync(CancellationToken.None);
        Assert.False(pipeline.PipelineFailed);
        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_TokenCancelledBeforeRun()
    {
        var ran = false;
        var step = new TestStep(_ => ran = true, ServiceProvider);

        var pipeline = CreatePipeline([step]);

        await pipeline.PrepareAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pipeline.RunAsync(new CancellationToken(true)));

        Assert.True(pipeline.PipelineFailed);
        Assert.False(ran);
    }
}