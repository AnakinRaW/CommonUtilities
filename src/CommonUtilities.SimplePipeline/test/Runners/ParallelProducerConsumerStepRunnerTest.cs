using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class ParallelProducerConsumerStepRunnerTest : StepRunnerTestBase<ParallelProducerConsumerStepRunner>
{
    public override bool PreservesStepExecutionOrder => false;

    protected override ParallelProducerConsumerStepRunner CreateStepRunner()
    {
        return CreateConsumerStepRunner();
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
    public async Task Run_AwaitDoesNotThrow()
    {
        var runner = CreateStepRunner();

        var ran2 = false;
        var s1 = new TestStep(_ => throw new Exception("Test"), ServiceProvider);
        var s2 = new TestStep(_ => ran2 = true, ServiceProvider);

        runner.AddStep(s1);
        runner.AddStep(s2);

        runner.Finish();
        
        await runner.RunAsync(CancellationToken.None);
        Assert.NotNull(runner.Exception);
        Assert.Equal("Test", runner.Exception.InnerExceptions.First()!.Message);
        Assert.True(ran2);

        Assert.Equivalent(new HashSet<IStep>([s1, s2]), runner.ExecutedSteps, true);
    }

    [Fact]
    public void Run_SyncWait_Throws()
    {
        var runner = CreateStepRunner();

        var ran2 = false;
        var s1 = new TestStep(_ => throw new Exception("Test"), ServiceProvider);
        var s2 = new TestStep(_ => ran2 = true, ServiceProvider);

        runner.AddStep(s1);
        runner.AddStep(s2);

        runner.Finish();
        
        runner.RunAsync(CancellationToken.None);

        Assert.Throws<AggregateException>(() => runner.Wait());

        Assert.NotNull(runner.Exception);
        Assert.Equal("Test", runner.Exception.InnerExceptions.First()!.Message);
        Assert.True(ran2);
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

        Task.Run(async () =>
        {
            runner.AddStep(s3);
            await Task.Delay(1000);
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
        var s2 = new TestStep(_ => Assert.Fail(), ServiceProvider);

        runner.AddStep(s1);

        var cts = new CancellationTokenSource();

        var runTask = runner.RunAsync(cts.Token);

        Task.Run(async () =>
        {
            await tcs.Task.ConfigureAwait(false);
            cts.Cancel();
            runner.AddStep(s2);
            runner.Finish();
        }, CancellationToken.None).Forget();


        await runTask;

        Assert.True(ran1);
        Assert.Equal([s1], runner.ExecutedSteps);

        Assert.True(runner.IsCancelled);
        Assert.NotNull(runner.Exception);

        Assert.IsType<OperationCanceledException>(runner.Exception.InnerExceptions.First(), true);
    }
}