using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class WaitStepTest : PipelineStepTestSuite
{ 
    protected override bool StepRespectsCancellationToken => false; // WaitStep ignores cancellation!
    protected override bool StepAddsExceptionsToErrorProperty => false; // Transforms to StopRunnerException

    protected override Type GetExpectedExceptionType(Exception thrownException)
    {
        // WaitStep transforms runner exceptions into StopRunnerException
        return typeof(StopRunnerException);
    }
    
    protected override PipelineStep CreateStep()
    {
        var runner = new AsyncStepRunner(1, ServiceProvider);
        return new WaitStep(runner, ServiceProvider);
    }

    protected override PipelineStep CreateStepWithAction(Func<CancellationToken, Task> action)
    {
        var runner = new AsyncStepRunner(1, ServiceProvider);
        runner.AddStep(new TestStep(action, ServiceProvider));
        _ = runner.RunAsync(CancellationToken.None);
        return new WaitStep(runner, ServiceProvider);
    }

    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitStep(null!, ServiceProvider));
        Assert.Throws<ArgumentNullException>(() => new WaitStep(
            new AsyncStepRunner(1, ServiceProvider),
            null!));
    }

    [Fact]
    public void ToString_HasExpectedValue()
    {
        var runner = new AsyncStepRunner(1, ServiceProvider);
        var step = new WaitStep(runner, ServiceProvider);
        Assert.Equal("Waiting for other step runner", step.ToString());
    }

    [Fact]
    public async Task RunAsync_EmptyRunner_CompletesSuccessfully()
    {
        var runner = new AsyncStepRunner(1, ServiceProvider);
        // No steps added

        var step = new WaitStep(runner, ServiceProvider);

        await runner.RunAsync(CancellationToken.None);
        await step.RunAsync(CancellationToken.None);

        Assert.Null(step.Error);
    }

    [Fact]
    public async Task RunAsync_CompletesAfterRunnerFinishes()
    {
        var runner = new AsyncStepRunner(2, ServiceProvider);

        var completed1 = false;
        var completed2 = false;
        runner.AddStep(new TestStep(_ =>
        {
            completed1 = true;
            return Task.CompletedTask;
        }, ServiceProvider));
        runner.AddStep(new TestStep(_ =>
        {
            completed2 = true;
            return Task.CompletedTask;
        }, ServiceProvider));

        var step = new WaitStep(runner, ServiceProvider);

        _ = runner.RunAsync(CancellationToken.None);

        await step.RunAsync(CancellationToken.None);

        Assert.False(runner.IsRunning);
        Assert.True(completed1);
        Assert.True(completed2);
    }

    [Fact]
    public async Task Wait_RunnerWithException_ThrowsStopRunnerException()
    {
        var runner = new AsyncStepRunner(1, ServiceProvider);
        runner.AddStep(new TestStep(_ => throw new InvalidOperationException("Test"), ServiceProvider));

        var step = new WaitStep(runner, ServiceProvider);

        _ = runner.RunAsync(CancellationToken.None);

        await Assert.ThrowsAsync<StopRunnerException>(() => step.RunAsync(CancellationToken.None));
        Assert.Null(step.Error);
    }

    [Fact]
    public async Task Wait_RunnerCancelled_ThrowsStopRunnerException()
    {
        var runner = new AsyncStepRunner(1, ServiceProvider);
        var cts = new CancellationTokenSource();

        runner.AddStep(new TestStep(ct =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }, ServiceProvider));

        var step = new WaitStep(runner, ServiceProvider);

        cts.Cancel();
        _ = runner.RunAsync(cts.Token);

        await Assert.ThrowsAsync<StopRunnerException>(() => step.RunAsync(CancellationToken.None));
        Assert.Null(step.Error);
    }

    [Fact]
    public async Task Wait_RunnerWithStopRunnerException_PropagatesStopRunnerException()
    {
        var runner = new AsyncStepRunner(1, ServiceProvider);
        runner.AddStep(new TestStep(_ => throw new StopRunnerException(), ServiceProvider));

        var step = new WaitStep(runner, ServiceProvider);
        _ = runner.RunAsync(CancellationToken.None);

        await Assert.ThrowsAsync<StopRunnerException>(() => step.RunAsync(CancellationToken.None));
        Assert.Null(step.Error);
    }
}