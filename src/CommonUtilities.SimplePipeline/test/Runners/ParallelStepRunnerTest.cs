using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class ParallelStepRunnerTest : StepRunnerTestBase<ParallelStepRunner>
{
    public override bool PreservesStepExecutionOrder => false;

    protected override ParallelStepRunner CreateStepRunner()
    {
        return CreateParallelStepRunner();
    }

    private ParallelStepRunner CreateParallelStepRunner(int workerCount = 2)
    {
        return new ParallelStepRunner(workerCount, ServiceProvider);
    }

    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ParallelStepRunner(1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParallelStepRunner(new Random().Next(int.MinValue, 0), ServiceProvider));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParallelStepRunner(0, ServiceProvider));
    }

    [Fact]
    public void Wait()
    {
        var runner = CreateParallelStepRunner();

        var ran1 = false;
        var ran2 = false;

        var step1 = new TestStep(_ => ran1 = true, ServiceProvider);
        var step2 = new TestStep(_ => ran2 = true, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        var runnerTask = runner.RunAsync(CancellationToken.None);
        runner.Wait();

        Assert.True(runnerTask.IsCompleted);

        Assert.True(ran1);
        Assert.True(ran2);
    }

    [Fact]
    public void Wait_FailedRunner_Throws()
    {
        var runner = CreateParallelStepRunner();

        var ran1 = false;
        var ran2 = false;

        var step1 = new TestStep(_ =>
        {
            ran1 = true;
            throw new Exception("Test");
        }, ServiceProvider);
        var step2 = new TestStep(_ => ran2 = true, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        var runnerTask = runner.RunAsync(CancellationToken.None);

        var e = Assert.Throws<AggregateException>(() => runner.Wait());
        Assert.Equal("Test", e.InnerExceptions.First().Message);

        Assert.True(runnerTask.IsCompleted);
        Assert.False(runnerTask.IsFaulted);

        Assert.NotNull(runner.Exception);

        Assert.True(ran1);
        Assert.True(ran2);
    }

    [Fact]
    public async Task RunAsync_Await()
    {
        var runner = CreateParallelStepRunner();

        var b = new ManualResetEvent(false);

        var ran1 = false;
        var ran2 = false;
        
        var step1 = new TestStep(_ =>
        {
            b.WaitOne();
            ran1 = true;
        }, ServiceProvider);
        var step2 = new TestStep(_ =>
        {
            b.WaitOne();
            ran2 = true;
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        var runTask = runner.RunAsync(CancellationToken.None);

        Assert.False(ran1);
        Assert.False(ran2);

        b.Set();

        await runTask;

        Assert.True(ran1);
        Assert.True(ran2);

        Assert.Equivalent(new HashSet<IStep>([step1, step2]), runner.ExecutedSteps, true);
    }

    [Fact]
    public async Task Wait_Timeout_ThrowsTimeoutException()
    {
        var runner = CreateParallelStepRunner();

        var b = new ManualResetEvent(false);

        var step1 = new TestStep(_ =>
        {
            b.WaitOne();
        }, ServiceProvider);

        runner.AddStep(step1);

        var runnerTask = runner.RunAsync(CancellationToken.None);

        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromMilliseconds(100)));

        // Even if Wait throws, we still continue executing
        b.Set();
        await runnerTask;

        Assert.True(runnerTask.IsCompleted);
    }

    [Fact]
    public async Task RunAsync_Cancelled()
    {
        // Make the result deterministic
        var runner = CreateParallelStepRunner(1);

        var cts = new CancellationTokenSource();

        var b = new ManualResetEvent(false);

        StepErrorEventArgs? error = null!;
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

        var step2 = new TestStep(_ =>
        {
            Assert.Fail();
        }, ServiceProvider);
        
        runner.AddStep(step1);
        runner.AddStep(step2);
        await runner.RunAsync(cts.Token);

        Assert.NotNull(error);
        Assert.True(error.Cancel);
        Assert.True(ran1);
        Assert.True(runner.IsCancelled);
    }
}