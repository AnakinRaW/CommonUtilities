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
    protected abstract T CreateStepRunner();

    public abstract bool PreservesStepExecutionOrder { get; }

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

        StepErrorEventArgs? error = null;
        runner.Error += (s, e) =>
        {
            Assert.Same(runner, s);
            error = e;
        };

        var step = new TestStep(_ =>
        {
            Assert.Fail();
        }, ServiceProvider);

        runner.AddStep(step);

        FinishAdding(runner);

        await runner.RunAsync(cts.Token);

        Assert.NotNull(error);
        Assert.True(error.Cancel);
        Assert.Same(step, error.Step);

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

        var ran = false;
        var step = new TestStep(_ =>
        {
            ran = true;
            throw new Exception("Test");
        }, ServiceProvider);

        runner.AddStep(step);

        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(error);
        Assert.False(error.Cancel);
        Assert.Same(step, error.Step);

        Assert.True(ran);
        Assert.Equal("Test", step.Error!.Message);
    }

    [Fact]
    public async Task RunAsync()
    {
        var runner = CreateStepRunner();

        runner.Error += (_, _) =>
        {
            Assert.Fail();
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

        if (PreservesStepExecutionOrder)
            Assert.Equal(["Step1", "Step2", "Step3"], ranList);
        else
            Assert.Equivalent(new HashSet<string>(["Step1", "Step2", "Step3"]), ranList, true);
        
        Assert.Equivalent(new ReadOnlyCollection<IStep>([step1, step2, step3]), runner.ExecutedSteps, true);
    }

    [Fact]
    public async Task RunAsync_Cancellation()
    {
        var runner = CreateStepRunner();

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
        var step2 = new TestStep(_ => ranList.Add("Step1"), ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        var runnerTask = runner.RunAsync(cts.Token);

        FinishAdding(runner);

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