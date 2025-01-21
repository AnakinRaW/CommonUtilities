using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class ParallelProducerConsumerStepRunnerTest : ParallelStepRunnerTestBase<ParallelProducerConsumerStepRunner>
{
    protected override ParallelProducerConsumerStepRunner CreateParallelRunner(int workerCount = 2)
    {
        return CreateConsumerStepRunner(workerCount);
    }

    private ParallelProducerConsumerStepRunner CreateConsumerStepRunner(int workerCount = 2)
    {
        return new ParallelProducerConsumerStepRunner(workerCount, ServiceProvider);
    }

    protected override void FinishAdding(ParallelProducerConsumerStepRunner runner)
    {
        runner.Finish();
    }

    [Fact]
    public async Task Run_WaitNotFinished()
    {
        var runner = CreateStepRunner();

        var tsc1 = new TaskCompletionSource<int>();
        var tsc2 = new TaskCompletionSource<int>();

        var s1 = new TestStep(_ => tsc1.SetResult(1), ServiceProvider);
        var s2 = new TestStep(_ => tsc2.SetResult(1), ServiceProvider);

        runner.AddStep(s1);
        runner.AddStep(s2);

        _ = runner.RunAsync(CancellationToken.None);

        await tsc1.Task;
        await tsc1.Task;

        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromSeconds(2)));
    }


    [Fact]
    public async Task Run_WaitNotFinished_CancellationShouldFinish()
    {
        var runner = CreateStepRunner();

        var cts = new CancellationTokenSource();
        var task = runner.RunAsync(cts.Token);

        // Give it some time
        await Task.Delay(500, CancellationToken.None);

        cts.Cancel();

        await task;

        Assert.True(runner.IsCancelled);
        Assert.NotNull(runner.Exception);
        Assert.IsType<OperationCanceledException>(runner.Exception.InnerExceptions.First(), true);
    }

    [Fact]
    public void Run_AddDelayed()
    {
        var runner = CreateStepRunner();

        var ran1 = false;
        var ran2 = false;
        var ran3 = false;
        var s1 = new TestStep(_ => ran1 = true, ServiceProvider);
        var s2 = new TestStep(_ => ran2 = true, ServiceProvider);
        var s3 = new TestStep(_ => ran3 = true, ServiceProvider);

        runner.AddStep(s1);
        runner.AddStep(s2);
        
        _ = runner.RunAsync(CancellationToken.None);

        Task.Run(() =>
        {
            runner.AddStep(s3);
            Task.Delay(1000);
            runner.Finish();
        });

        runner.Wait();

        Assert.True(ran1);
        Assert.True(ran2);
        Assert.True(ran3);
    }

    [Fact]
    public async Task Run_AddDelayed_Await()
    {
        var runner = CreateStepRunner();

        var ran1 = false;
        var ran2 = false;
        var ran3 = false;
        var s1 = new TestStep(_ => ran1 = true, ServiceProvider);
        var s2 = new TestStep(_ => ran2 = true, ServiceProvider);
        var s3 = new TestStep(_ => ran3 = true, ServiceProvider);

        runner.AddStep(s1);
        runner.AddStep(s2);
       
        var runTask = runner.RunAsync(CancellationToken.None);

        Task.Run(() =>
        {
            runner.AddStep(s3);
            runner.Finish();

        }).Forget();

        await runTask;
        // Should not block
        runner.Wait();

        Assert.True(ran1);
        Assert.True(ran2);
        Assert.True(ran3);
    }

    [Fact]
    public async Task Run_AddDelayed_Cancelled()
    {
        var runner = CreateStepRunner();

        var tcs = new TaskCompletionSource<int>();

        var ran1 = false;
        var s1 = new TestStep(_ =>
        {
            ran1 = true;
            tcs.SetResult(0);
        }, ServiceProvider);
        var ran2 = false;
        var s2 = new TestStep(_ => ran2 = true, ServiceProvider);

        runner.AddStep(s1);

        var cts = new CancellationTokenSource();

        var runTask = runner.RunAsync(cts.Token);

        Task.Run(async () =>
        {
            await tcs.Task.ConfigureAwait(false);
            await Task.Delay(1000, CancellationToken.None); // Give it some time, so ensure the runner is internally blocking and waiting for the next step.
            cts.Cancel();
            runner.AddStep(s2);
        }, CancellationToken.None).Forget();


        await runTask;

        Assert.True(ran1);
        Assert.False(ran2);
        Assert.Equal([s1], runner.ExecutedSteps);
        
        Assert.True(runner.IsCancelled);
        Assert.NotNull(runner.Exception);

        Assert.IsType<OperationCanceledException>(runner.Exception.InnerExceptions.First(), true);
    }

    [Fact]
    public void AddStep_AddAfterFinish()
    {
        var runner = CreateStepRunner();
        var s1 = new TestStep(_ => { }, ServiceProvider);
        runner.AddStep(s1);

        runner.Finish();

        Assert.Throws<InvalidOperationException>(() => runner.AddStep(s1));
    }

    [Fact]
    public async Task RunAsync_Cancelled()
    {
        // Have deterministic result
        var runner = CreateParallelRunner(1);

        var cts = new CancellationTokenSource();

        var b = new ManualResetEvent(false);

        StepRunnerErrorEventArgs? error = null!;
        runner.Error += (_, e) =>
        {
            error = e;
        };

        var ran1 = false;
        var step1 = new TestStep(_ =>
        {
            ran1 = true;
            cts.Cancel();
            b.Set();
        }, ServiceProvider);

        var ran2 = false;
        var step2 = new TestStep(_ => ran2 = true, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        FinishAdding(runner);

        await runner.RunAsync(cts.Token);

        // This is all we can test for, because we cannot know whether the cancellation was requested while fetching step queue data
        // or the step was already fetched
        Assert.True(runner.IsCancelled);
    }
}