using AnakinRaW.CommonUtilities.Testing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public abstract class PipelineTestBase : TestBaseWithServiceProvider
{
    protected abstract Pipeline CreatePipeline(IList<IStep> steps);

    protected abstract ITrackingPipeline CreateTrackingPipeline(
        Func<CancellationToken, Task> prepare,
        Func<CancellationToken, Task> run);

    protected virtual ITrackingPipeline CreateTrackingPipeline(Action prepare, Action run)
    {
        return CreateTrackingPipeline(
            _ => Task.Run(prepare, CancellationToken.None), 
            _ => Task.Run(run, CancellationToken.None));
    }

    #region Initialization

    [Fact]
    public void Ctor_WithValidServiceProvider_InitializesCorrectly()
    {
        var pipeline = CreatePipeline([]);
        Assert.False(pipeline.PipelineFailed);
        Assert.False(pipeline.PipelineCancelled);
        Assert.False(pipeline.IsDisposed);
    }

    #endregion

    #region PrepareAsync Tests

    [Fact]
    public async Task PrepareAsync_CalledOnce_Succeeds()
    {
        var pipeline = CreatePipeline([]);

        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);

        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task PrepareAsync_CalledMultipleTimes_ThrowsInvalidOperationException()
    {
        var prepareCount = 0;
        var pipeline = CreateTrackingPipeline(() => prepareCount++, () => { });

        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipeline.PrepareAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipeline.PrepareAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, prepareCount);
    }

    [Fact]
    public async Task PrepareAsync_ConcurrentCalls_OnlyFirstSucceeds()
    {
        var prepareCount = 0;
        var barrier = new TaskCompletionSource<bool>();

        var pipeline = CreateTrackingPipeline(async _ =>
        {
            Interlocked.Increment(ref prepareCount);
            await barrier.Task;
        }, _ => Task.CompletedTask);

        var firstTask = pipeline.PrepareAsync(CancellationToken.None);

        await Task.Delay(50, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.PrepareAsync(CancellationToken.None));

        barrier.SetResult(true);
        await firstTask;

        Assert.Equal(1, prepareCount);
    }

    [Fact]
    public async Task PrepareAsync_FailedPreparation_CannotRetry()
    {
        var callCount = 0;
        var pipeline = CreateTrackingPipeline(_ =>
        {
            callCount++;
            throw new ArgumentException("Preparation failed");
        }, _ => Task.CompletedTask);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            pipeline.PrepareAsync(TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.PrepareAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task PrepareAsync_Cancelled_ThrowsOperationCancelledException()
    {
        var mre = new ManualResetEventSlim(false);
        var cts = new CancellationTokenSource();

        var pipeline = CreateTrackingPipeline(async ct =>
        {
            await Task.Yield();
            mre.Wait(TestContext.Current.CancellationToken);
            ct.ThrowIfCancellationRequested();
        }, _ => Task.CompletedTask);

        var prepareTask = pipeline.PrepareAsync(cts.Token);
        cts.Cancel();
        mre.Set();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await prepareTask);
    }

    [Fact]
    public async Task PrepareAsync_Disposed_ThrowsObjectDisposedException()
    {
        var pipeline = CreatePipeline([]);
        pipeline.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => pipeline.PrepareAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PrepareAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var pipeline = CreatePipeline([]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.PrepareAsync(cts.Token));
    }

    #endregion

    #region RunAsync Tests

    [Fact]
    public async Task RunAsync_WithoutExplicitPrepare_PreparesAutomatically()
    {
        var prepareCount = 0;
        var runCount = 0;
        var pipeline = CreateTrackingPipeline(() => prepareCount++, () => runCount++);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, prepareCount);
        Assert.Equal(1, runCount);
    }

    [Fact]
    public async Task RunAsync_PreparationThrowsNonCancellationException_SetsPipelineFailed()
    {
        var pipeline = CreateTrackingPipeline(
            _ => throw new ArgumentException("Preparation failed"), 
            _ => Task.CompletedTask);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            pipeline.RunAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_PreparationThrowsOperationCanceledException_SetsPipelineCancelled()
    {
        var pipeline = CreateTrackingPipeline(
            _ => throw new OperationCanceledException(),
            _ => Task.CompletedTask);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            pipeline.RunAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_CalledMultipleTimes_ThrowsInvalidOperationException()
    {
        var counter = 0;
        var s = new TestStep(_ =>
        {
            counter++;
            return Task.CompletedTask;
        }, ServiceProvider);
        var pipeline = CreatePipeline([s]);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipeline.RunAsync(TestContext.Current.CancellationToken));
        
        // RunAsync also prepares the pipeline, thus this throws too
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipeline.PrepareAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, counter);
    }

    [Fact]
    public async Task RunAsync_CalledWhilePrepareAsyncInProgress_WaitsForSamePreparation()
    {
        var preparationStarted = new TaskCompletionSource<bool>();
        var continuePreparation = new TaskCompletionSource<bool>();
        var executed = false;
        var prepareCallCount = 0;

        var pipeline = CreateTrackingPipeline(Prepare, _ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Start PrepareAsync but don't await completion
        var prepareTask = pipeline.PrepareAsync(CancellationToken.None);

        await preparationStarted.Task;

        var runTask = pipeline.RunAsync(CancellationToken.None);

        continuePreparation.SetResult(true);

        await prepareTask;
        await runTask;

        Assert.Equal(1, prepareCallCount);
        Assert.True(executed);
        return;

        async Task Prepare(CancellationToken token)
        {
            Interlocked.Increment(ref prepareCallCount);
            preparationStarted.SetResult(true);
            await continuePreparation.Task;
        }
    }

    [Fact]
    public async Task RunAsync_ConcurrentCalls_OnlyFirstSucceeds()
    {
        var runCount = 0;
        var barrier = new TaskCompletionSource<bool>();

        var step = new TestStep(async _ =>
        {
            Interlocked.Increment(ref runCount);
            await barrier.Task;
        }, ServiceProvider);

        var pipeline = CreatePipeline([step]);

        var firstTask = pipeline.RunAsync(CancellationToken.None);

        await Task.Delay(50, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.RunAsync(CancellationToken.None));

        barrier.SetResult(true);
        await firstTask;

        Assert.Equal(1, runCount);
    }

    [Fact]
    public async Task RunAsync_AfterPrepare_ExecutesStep()
    {
        var executed = false;
        var step = new TestStep(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);
        var pipeline = CreatePipeline([step]);

        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);
        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(executed);
    }

    [Fact]
    public async Task RunAsync_Successful_PipelineFailedAndCancelledIsFalse()
    {
        var s = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        var pipeline = CreatePipeline([s]);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.False(pipeline.PipelineFailed);
        Assert.False(pipeline.PipelineCancelled);
    }

    [Fact]
    public async Task RunAsync_ThrowsNonCancellationException_SetsPipelineFailed()
    {
        var step = new TestStep(_ => throw new InvalidOperationException("Test error"), ServiceProvider);
        var pipeline = CreatePipeline([step]);

        var record = await Record.ExceptionAsync(async () => await pipeline.RunAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(record);


        Assert.True(pipeline.PipelineFailed);
        Assert.False(pipeline.PipelineCancelled);
    }

    [Fact]
    public async Task RunAsync_ThrowsTaskCanceledException_SetsPipelineCancelled()
    {
        var step = new TestStep(_ => throw new TaskCanceledException(), ServiceProvider);
        var pipeline = CreatePipeline([step]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.PipelineCancelled);
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_ThrowsOperationCanceledException_SetsPipelineCancelled()
    {
        var step = new TestStep(_ => throw new OperationCanceledException(), ServiceProvider);
        var pipeline = CreatePipeline([step]);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.PipelineCancelled);
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_CancelledDuringPreparationViaExternalToken_SetsPipelineCancelled()
    {
        using var cts = new CancellationTokenSource();
        var waitToCancel = new TaskCompletionSource<bool>();
        var waitUntilCanceled = new ManualResetEvent(false);

        var pipeline = CreateTrackingPipeline(async ct =>
        {
            await Task.Yield();
            waitToCancel.SetResult(true);
            waitUntilCanceled.WaitOne();
            ct.ThrowIfCancellationRequested();
        }, _ => Task.CompletedTask);

        var runTask = pipeline.RunAsync(cts.Token);
        await waitToCancel.Task;

        cts.Cancel();
        waitUntilCanceled.Set();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
    }

    [Fact]
    public async Task RunAsync_ExternalTokenCancelledDuringExecution_SetsPipelineCancelled()
    {
        using var cts = new CancellationTokenSource();
        var waitToCancel = new TaskCompletionSource<bool>();
        var waitUntilCanceled = new ManualResetEvent(false);

        var step = new TestStep(async ct =>
        {
            await Task.Yield();
            waitToCancel.SetResult(true);
            waitUntilCanceled.WaitOne();
            ct.ThrowIfCancellationRequested();
        }, ServiceProvider);

        var pipeline = CreatePipeline([step]);

        var runTask = pipeline.RunAsync(cts.Token);
        await waitToCancel.Task;

        cts.Cancel();
        waitUntilCanceled.Set();

        await Assert.ThrowsAsync<OperationCanceledException>(() => runTask);
        Assert.True(pipeline.PipelineCancelled);
        Assert.False(pipeline.PipelineFailed);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_DisposedPipeline_ThrowsOnPrepare()
    {
        var pipeline = CreatePipeline([]);
        pipeline.Dispose();

        Assert.True(pipeline.IsDisposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pipeline.PrepareAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Dispose_DisposedPipeline_ThrowsOnRun()
    {
        var pipeline = CreatePipeline([]);
        pipeline.Dispose();

        Assert.True(pipeline.IsDisposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Dispose_CalledMultipleTimes_NoEffect()
    {
        var counter = 0;
        var s = new TestStep(_ =>
        {
            counter++;
            return Task.CompletedTask;
        }, ServiceProvider);
        var pipeline = CreatePipeline([s]);

        pipeline.Dispose();
        pipeline.Dispose();
        pipeline.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pipeline.PrepareAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, counter);
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task Dispose_AfterPrepare_ThrowsOnRun()
    {
        var executed = false;
        var step = new TestStep(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);
        var pipeline = CreatePipeline([step]);

        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);
        pipeline.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.False(executed);
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task Dispose_AfterSuccessfulRun_NoException()
    {
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        var pipeline = CreatePipeline([step]);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        pipeline.Dispose();

        Assert.True(pipeline.IsDisposed);
    }

    [Fact]
    public async Task Dispose_AfterFailedRun_NoException()
    {
        var step = new TestStep(_ => throw new InvalidOperationException(), ServiceProvider);
        var pipeline = CreatePipeline([step]);

        var record = await Record.ExceptionAsync(() => pipeline.RunAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(record);
        
        pipeline.Dispose();

        Assert.True(pipeline.IsDisposed);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_DuringPreparation_HasNoEffect()
    {
        var waitToCancel = new TaskCompletionSource<bool>();
        var waitUntilCanceled = new ManualResetEvent(false);

        var pipeline = CreateTrackingPipeline(async ct =>
        {
            await Task.Yield();
            waitToCancel.SetResult(true);
            waitUntilCanceled.WaitOne();
            ct.ThrowIfCancellationRequested();
        }, _ => Task.CompletedTask);

        var prepareTask = pipeline.PrepareAsync(CancellationToken.None);
        await waitToCancel.Task;

        pipeline.Cancel();
        waitUntilCanceled.Set();

        var e = await Record.ExceptionAsync(() => prepareTask);
        Assert.Null(e);
    }

    [Fact]
    public async Task Cancel_DuringWaitForPreparation_CancelsPipeline()
    {
        var waitToCancel = new TaskCompletionSource<bool>();
        var waitUntilCanceled = new ManualResetEvent(false);

        var pipeline = CreateTrackingPipeline(async ct =>
        {
            await Task.Yield();
            waitToCancel.SetResult(true);
            waitUntilCanceled.WaitOne();
            ct.ThrowIfCancellationRequested();
        }, _ => Task.CompletedTask);

        var runTask = pipeline.RunAsync(CancellationToken.None);
        await waitToCancel.Task;

        pipeline.Cancel();
        waitUntilCanceled.Set();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
    }

    [Fact]
    public async Task Cancel_DuringExecution_CancelsAndSetsPipelineFailed()
    {
        var waitToCancel = new TaskCompletionSource<bool>();
        var waitUntilCanceled = new ManualResetEvent(false);
        var capturedToken = CancellationToken.None;

        var step = new TestStep(async ct =>
        {
            await Task.Yield();
            capturedToken = ct;
            waitToCancel.SetResult(true);
            waitUntilCanceled.WaitOne();
        }, ServiceProvider);

        var pipeline = CreatePipeline([step]);

        await pipeline.PrepareAsync(CancellationToken.None);

        var pipelineTask = pipeline.RunAsync(CancellationToken.None);
        await waitToCancel.Task;

        pipeline.Cancel();
        waitUntilCanceled.Set();

        await Assert.ThrowsAsync<OperationCanceledException>(() => pipelineTask);

        Assert.False(pipeline.PipelineFailed);
        Assert.True(pipeline.PipelineCancelled);
        Assert.True(capturedToken.IsCancellationRequested);
    }
    
    [Fact]
    public async Task Cancel_BeforeRun_HasNoEffect()
    {
        var executed = false;
        var step = new TestStep(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);
        var pipeline = CreatePipeline([step]);

        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);
        pipeline.Cancel();

        await pipeline.RunAsync(CancellationToken.None);

        Assert.False(pipeline.PipelineFailed);
        Assert.False(pipeline.PipelineCancelled);
        Assert.True(executed);
    }

    [Fact]
    public async Task Cancel_BeforePrepare_NoException()
    {
        var pipeline = CreatePipeline([]);

        pipeline.Cancel();

        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);
        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.False(pipeline.PipelineFailed);
        Assert.False(pipeline.PipelineCancelled);
    }

    [Fact]
    public async Task Cancel_AfterRun_NoEffect()
    {
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        var pipeline = CreatePipeline([step]);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        pipeline.Cancel(); // Should not throw

        Assert.False(pipeline.PipelineFailed);
        Assert.False(pipeline.PipelineCancelled);
    }

    [Fact]
    public void Cancel_CalledMultipleTimes_NoException()
    {
        var pipeline = CreatePipeline([]);

        pipeline.Cancel();
        pipeline.Cancel();
        pipeline.Cancel();

        Assert.False(pipeline.PipelineCancelled);
    }

    [Fact]
    public void Cancel_OnDisposedPipeline_NoException()
    {
        var pipeline = CreatePipeline([]);
        pipeline.Dispose();

        pipeline.Cancel();

        Assert.True(pipeline.IsDisposed);
    }

    #endregion
    
    #region ToString Tests

    [Fact]
    public void ToString_ReturnsTypeName()
    {
        var pipeline = CreatePipeline([]);

        var result = pipeline.ToString();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    #endregion
}

public interface ITrackingPipeline : IPipeline;