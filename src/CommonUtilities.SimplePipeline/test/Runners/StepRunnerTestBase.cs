using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public abstract class StepRunnerTestBase<T> : CommonTestBase where T : IStepRunner
{
    public abstract bool PreservesStepExecutionOrder { get; }

    protected abstract T CreateStepRunner(bool deterministic = false);

    protected virtual void FinishAdding(T runner)
    {
    }

    [Fact]
    public void AddStep_Null_Throws()
    {
        var runner = CreateStepRunner();
        Assert.Throws<ArgumentNullException>(() => runner.AddStep(null!));
    }

    [Fact]
    public async Task RunAsync_StepsEmpty()
    {
        var runner = CreateStepRunner();
        FinishAdding(runner);
        await runner.RunAsync(CancellationToken.None);
        Assert.Empty(runner.ExecutedSteps);
    }

    [Fact]
    public async Task RunAsync_CancelledWhenStarted()
    {
        var runner = CreateStepRunner();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var ran = false;
        var step = new TestStep(_ => ran = true, ServiceProvider);

        runner.AddStep(step);

        FinishAdding(runner);

        await runner.RunAsync(cts.Token);

        Assert.False(ran);
        Assert.Empty(runner.ExecutedSteps);
    }

    [Fact]
    public async Task RunAsync_WithError()
    {
        var runner = CreateStepRunner();

        StepErrorEventArgs? error = null;
        runner.Error += (s, e) =>
        {
            Assert.Same(runner, s);
            error = e;
        };

        var ran1 = false;
        var ran2 = false;
        var step1 = new TestStep(_ =>
        {
            ran1 = true;
            throw new Exception("Test");
        }, ServiceProvider);
        var step2 = new TestStep(_ =>
        {
            ran2 = true;
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(error);
        Assert.False(error.Cancel);
        Assert.Same(step1, error.Step);

        Assert.True(ran1);
        Assert.True(ran2);
        Assert.Equal("Test", step1.Error!.Message);
    }

    [Fact]
    public async Task RunAsync()
    {
        var runner = CreateStepRunner();

        var hasError = false;
        runner.Error += (_, _) =>
        {
            hasError = true;
        };

        var ranList = new List<string>();
        var tsc = new ManualResetEventSlim(false);
        var step1 = new TestStep(_ =>
        {
            ranList.Add("Step1");
            tsc.Wait();
        }, ServiceProvider);
        var step2 = new TestStep(_ => ranList.Add("Step2"), ServiceProvider);
        
        runner.AddStep(step1);
        runner.AddStep(step2);

        var runnerTask = runner.RunAsync(CancellationToken.None);

        // Step that was added later, also gets executed
        var step3 = new TestStep(_ => ranList.Add("Step3"), ServiceProvider);
        runner.AddStep(step3);
        tsc.Set();

        FinishAdding(runner);

        await runnerTask;

        Assert.False(hasError);

        if (PreservesStepExecutionOrder)
            Assert.Equal(["Step1", "Step2", "Step3"], ranList);
        else
            Assert.Equivalent(new HashSet<string>(["Step1", "Step2", "Step3"]), ranList, true);
        
        Assert.Equivalent(new ReadOnlyCollection<IStep>([step1, step2, step3]), runner.ExecutedSteps, true);
    }

    [Fact]
    public async Task RunAsync_Cancellation()
    {
        var runner = CreateStepRunner(true);

        var ranList = new List<string>();
        var cts = new CancellationTokenSource();
        var step1 = new TestStep(_ =>
        {
            Task.Delay(1000).Wait();
            ranList.Add("Step1");
            cts.Cancel();

        }, ServiceProvider);
        var step2 = new TestStep(_ => ranList.Add("Step2"), ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        FinishAdding(runner);

        var runnerTask = runner.RunAsync(cts.Token);

        await runnerTask;

        Assert.Equal(["Step1"], ranList);
        Assert.Contains(step1, runner.ExecutedSteps);
        Assert.DoesNotContain(step2, runner.ExecutedSteps);
    }

    [Fact]
    public async Task RunAsync_StopRunner_ShouldStopExecution()
    {
        var runner = CreateStepRunner(true);

        StepErrorEventArgs? args = null!;
        runner.Error += (_, e) =>
        {
            args = e;
        };

        var ranList = new List<string>();
        var cts = new CancellationTokenSource();
        var step1 = new TestStep(_ =>
        {
            Task.Delay(1000).Wait();
            ranList.Add("Step1");
            cts.Cancel();

        }, ServiceProvider);
        var step2 = new TestStep(_ => ranList.Add("Step2"), ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        FinishAdding(runner);

        var runnerTask = runner.RunAsync(cts.Token);

        await runnerTask;

        if (PreservesStepExecutionOrder)
        {
            Assert.NotNull(args);
            Assert.True(args.Cancel);

            Assert.Equal(["Step1"], ranList);

            Assert.Contains(step1, runner.ExecutedSteps);
            Assert.DoesNotContain(step2, runner.ExecutedSteps);
        }
    }
}