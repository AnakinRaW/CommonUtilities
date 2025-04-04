using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public abstract class ParallelStepRunnerTestBase<T> : StepRunnerTestBase<T> where T : class, IParallelStepRunner
{
    public override bool PreservesStepExecutionOrder => false;

    protected abstract T CreateParallelRunner(int workerCount = 2);

    protected sealed override T CreateStepRunner(bool deterministic = false)
    {
        return CreateParallelRunner(deterministic ? 1 : new Random().Next(2, 6));
    }

    [Fact]
    public void Ctor_WorkerCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateParallelRunner(new Random().Next(65, int.MaxValue)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateParallelRunner(new Random().Next(int.MinValue, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateParallelRunner(0));

        var numRunners = new Random().Next(1, 64);
        var runner = CreateParallelRunner(numRunners);

        Assert.Equal(numRunners, runner.WorkerCount);
    }

    [Fact]
    public void Wait()
    {
        var runner = CreateParallelRunner();

        var ran1 = false;
        var ran2 = false;

        var step1 = new TestStep(_ => ran1 = true, ServiceProvider);
        var step2 = new TestStep(_ => ran2 = true, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        FinishAdding(runner);

        runner.RunAsync(CancellationToken.None).Forget();
        runner.Wait();

        // We cannot assert on the returned task,
        // as the impl. creates different tasks for await and Wait().
        // This may result in a race where the Wait() reports completion before the awaitable task

        Assert.True(ran1);
        Assert.True(ran2);
    }

    [Fact]
    public void Wait_FailedRunner_Throws()
    {
        var runner = CreateParallelRunner();

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

        FinishAdding(runner);

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
        var runner = CreateParallelRunner();

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

        FinishAdding(runner);

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
        var runner = CreateParallelRunner();

        var b = new ManualResetEvent(false);

        var step1 = new TestStep(_ =>
        {
            b.WaitOne();
        }, ServiceProvider);

        runner.AddStep(step1);

        FinishAdding(runner);

        var runnerTask = runner.RunAsync(CancellationToken.None);

        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromMilliseconds(100)));

        // Even if Wait throws, we still continue executing
        b.Set();
        await runnerTask;

        Assert.True(runnerTask.IsCompleted);
    }

    [Fact]
    public async Task RunAsync_ErrorSetsCancellation()
    {
        // Have deterministic result
        var runner = CreateParallelRunner(1);

        var step1 = new TestStep(_ => throw new StopRunnerException(), ServiceProvider);
        var ran2 = false;
        var step2 = new TestStep(_ => { ran2 = true; }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        // Do not signal runner to finish. It must do that on itself!

        var runnerTask = runner.RunAsync(CancellationToken.None);

        await runnerTask;

        Assert.False(ran2);
    }
}