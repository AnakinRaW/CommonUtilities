using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class PipelineStepTest : TestBaseWithServiceProvider
{
    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestStep(null, null!));
    }

    [Fact]
    public void Disposed()
    {
        var step = new TestStep(null, ServiceProvider);

        step.Dispose();
        Assert.True(step.IsDisposed);
    }

    [Fact]
    public async Task RunAsync_TaskAction()
    {
        var ran = false;
        var step = new TestStep(_ =>
        {
            ran = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        await step.RunAsync(CancellationToken.None);

        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_AwaitedAction()
    {
        var ran = false;
        var step = new TestStep(async _ =>
        {
            await Task.Yield();
            ran = true;
        }, ServiceProvider);

        await step.RunAsync(CancellationToken.None);

        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_ThrowsException()
    {
        var expectedError = new Exception();

        var step = new TestStep(_ => throw expectedError, ServiceProvider);

        await Assert.ThrowsAsync<Exception>(() => step.RunAsync(CancellationToken.None));
        Assert.Same(expectedError, step.Error);
    }

    [Fact]
    public async Task RunAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var step = new TestStep(ct =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }, ServiceProvider);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => step.RunAsync(cts.Token));
        Assert.Null(step.Error);
    }

    [Fact]
    public async Task RunAsync_StopRunnerException_IsNotAddedToErrors()
    {
        var step = new TestStep(_ => throw new StopRunnerException(), ServiceProvider);

        await Assert.ThrowsAsync<StopRunnerException>(() => step.RunAsync(CancellationToken.None));
        Assert.Null(step.Error);
    }

    [Fact]
    public async Task RunAsync_AggregateException()
    {
        var expected = new AggregateException(new Exception("Test"));
        var step = new TestStep(_ => throw expected, ServiceProvider);

        await Assert.ThrowsAsync<AggregateException>(() => step.RunAsync(CancellationToken.None));
        Assert.Same(expected, step.Error);
    }

    [Fact]
    public async Task RunAsync_AggregateException_OriginatedFromOperationCancelled()
    {
        var expected = new Exception("Test");
        var step = new TestStep(_ => throw new AggregateException(new OperationCanceledException(null, expected)), ServiceProvider);

        await Assert.ThrowsAsync<AggregateException>(() => step.RunAsync(CancellationToken.None));
        Assert.Same(expected, step.Error);
    }

    [Fact]
    public async Task RunAsync_AggregateException_OriginatedFromOperationCancelled_NoInnerException()
    {
        var step = new TestStep(_ => throw new AggregateException(new OperationCanceledException()), ServiceProvider);

        await Assert.ThrowsAsync<AggregateException>(() => step.RunAsync(CancellationToken.None));
        Assert.Null(step.Error);
    }

    [Fact]
    public async Task GetAwaiter_AfterCompletion_ReturnsImmediately()
    {
        var executed = false;
        var step = new TestStep(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        await step.RunAsync(CancellationToken.None);
        
        await step;
        Assert.True(executed);
    }

    [Fact]
    public async Task GetAwaiter_BeforeStart_WaitsForCompletion()
    {
        var tcs = new TaskCompletionSource<bool>();
        var step = new TestStep(async _ =>
        {
            await tcs.Task;
        }, ServiceProvider);

        var awaiterTask = Task.Run(async () => await step, TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaiterTask.IsCompleted);

        var runTask = step.RunAsync(CancellationToken.None);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaiterTask.IsCompleted);

        tcs.SetResult(true);
        await runTask;

        await awaiterTask;
        Assert.True(awaiterTask.IsCompleted);
    }

    [Fact]
    public async Task GetAwaiter_DuringExecution_WaitsForCompletion()
    {
        var tcs = new TaskCompletionSource<bool>();
        var step = new TestStep(async _ =>
        {
            await tcs.Task;
        }, ServiceProvider);

        var runTask = step.RunAsync(CancellationToken.None);

        var awaiterTask = Task.Run(async () => await step, TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaiterTask.IsCompleted);

        tcs.SetResult(true);
        await runTask;

        await awaiterTask;
        Assert.True(awaiterTask.IsCompleted);
    }

    [Fact]
    public async Task GetAwaiter_PropagatesException()
    {
        var expected = new InvalidOperationException("Test error");
        var step = new TestStep(_ => throw expected, ServiceProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => step.RunAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await step);
    }

    [Fact]
    public async Task GetAwaiter_MultipleAwaiters_AllComplete()
    {
        var tcs = new TaskCompletionSource<bool>();
        var step = new TestStep(async _ =>
        {
            await tcs.Task;
        }, ServiceProvider);

        var awaiter1 = Task.Run(async () => await step, TestContext.Current.CancellationToken);
        var awaiter2 = Task.Run(async () => await step, TestContext.Current.CancellationToken);
        var awaiter3 = Task.Run(async () => await step, TestContext.Current.CancellationToken);

        var runTask = step.RunAsync(CancellationToken.None);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaiter1.IsCompleted);
        Assert.False(awaiter2.IsCompleted);
        Assert.False(awaiter3.IsCompleted);

        tcs.SetResult(true);
        await runTask;

        await Task.WhenAll(awaiter1, awaiter2, awaiter3);
        Assert.True(awaiter1.IsCompleted);
        Assert.True(awaiter2.IsCompleted);
        Assert.True(awaiter3.IsCompleted);
    }

    [Fact]
    public async Task GetAwaiter_WithCancellation_PropagatesCancellation()
    {
        var step = new TestStep(ct =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }, ServiceProvider);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => step.RunAsync(cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await step);
    }

    [Fact]
    public async Task GetAwaiter_AfterSuccessfulRun_CanBeAwaitedMultipleTimes()
    {
        var executionCount = 0;
        var step = new TestStep(_ =>
        {
            Interlocked.Increment(ref executionCount);
            return Task.CompletedTask;
        }, ServiceProvider);

        await step.RunAsync(CancellationToken.None);
        Assert.Equal(1, executionCount);

        await step;
        await step;
        await step;

        Assert.Equal(1, executionCount);
    }


    [Fact]
    public void ToString_IsTypeName()
    {
        var step = new TestStep(null, ServiceProvider);
        Assert.Equal(step.GetType().Name, step.ToString());
    }
}