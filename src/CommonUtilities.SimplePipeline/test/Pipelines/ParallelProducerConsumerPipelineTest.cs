using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class ParallelProducerConsumerPipelineTest : PipelineTest
{
    [Fact]
    public async Task RunAsync_DelayedAdd()
    { 
        var tcs = new TaskCompletionSource<int>();
        
        var s1 = new TestStep(_ =>
        {
            Task.Delay(3000).Wait();
            tcs.SetResult(0);
        }, ServiceProvider);

        var s2Run = false;
        var s2 = new TestStep(_ =>
        {
            s2Run = true;
        }, ServiceProvider);

        var pipeline = CreateConsumerPipeline(ValueFunction());

        await pipeline.RunAsync();

        Assert.True(s2Run);

        return;

        async IAsyncEnumerable<IStep> ValueFunction()
        {
            yield return s1;
            await tcs.Task;
            yield return s2;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_DelayedAdd_PrepareFails(bool failFast)
    {
        var mre = new ManualResetEventSlim(false);

        var ran = false;
        var s1 = new TestStep(ct =>
        {
            mre.Wait(ct);
            ct.ThrowIfCancellationRequested();
            ran = true;

        }, ServiceProvider);
        var s2 = new TestStep(_ => { }, ServiceProvider);

        var pipeline = CreateConsumerPipeline(ValueFunction(), failFast);

        var task = Assert.ThrowsAsync<ApplicationException>(async () => await pipeline.RunAsync());

        if (failFast) 
            await task;

        mre.Set();

        await task;

        if (failFast) 
            Assert.False(ran);
        else
            Assert.True(ran);

        return;

        async IAsyncEnumerable<IStep> ValueFunction()
        {
            yield return s1;
            yield return s2;
            throw new ApplicationException("test");
        }
    }

    [Fact]
    public async Task PrepareAsync_PrepareFails()
    {
        var s1 = new TestStep(_ => { }, ServiceProvider);
        var s2 = new TestStep(_ => { }, ServiceProvider);

        var pipeline = CreateConsumerPipeline(ValueFunction());

        await Assert.ThrowsAsync<ApplicationException>(pipeline.PrepareAsync);
        
        return;

        async IAsyncEnumerable<IStep> ValueFunction()
        {
            yield return s1;
            yield return s2;
            throw new ApplicationException("test");
        }
    }


    [Fact]
    public async Task RunAsync_PrepareCancelled()
    {
        var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<int>();

        var s1 = new TestStep(_ =>
        {
            Task.Delay(3000).Wait();
            tcs.SetResult(0);
        }, ServiceProvider);

        var s2Run = false;
        var s2 = new TestStep(_ =>
        {
            s2Run = true;
        }, ServiceProvider);

        var pipeline = CreateConsumerPipeline(ValueFunction());
        
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pipeline.RunAsync(cts.Token));

        Assert.False(s2Run);
        
        return;

        async IAsyncEnumerable<IStep> ValueFunction()
        {
            yield return s1;
            await tcs.Task;
            cts.Cancel();
            await Task.Delay(1000);
            yield return s2;
        }
    }

    protected override Pipeline CreatePipeline(IList<IStep> steps)
    {
        return new TestParallelProducerConsumerPipeline(steps.ToAsyncEnumerable(), 4, true, ServiceProvider);
    }

    private Pipeline CreateConsumerPipeline(IAsyncEnumerable<IStep> steps, bool failFast = true)
    {
        return new TestParallelProducerConsumerPipeline(steps, 4, failFast, ServiceProvider);
    }

    private class TestParallelProducerConsumerPipeline(
        IAsyncEnumerable<IStep> steps,
        int workerCount,
        bool failFast,
        IServiceProvider serviceProvider)
        : ParallelProducerConsumerPipeline(workerCount, failFast, serviceProvider)
    {
        protected override IAsyncEnumerable<IStep> BuildSteps()
        {
            return steps;
        }
    }
}