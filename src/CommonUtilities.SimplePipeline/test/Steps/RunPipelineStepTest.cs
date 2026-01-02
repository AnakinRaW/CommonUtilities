using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class RunPipelineStepTest : PipelineStepTestBase
{
    protected override bool StepRespectsCancellationToken => true;

    protected override bool StepAddsExceptionsToErrorProperty => true;

    protected override bool StepAddsStopRunnerExceptionToErrorProperty => false;

    protected override PipelineStep CreateStep()
    {
        var steps = new List<IStep>();
        var pipeline = new TestPipeline(ServiceProvider, RunnerBehavior.Sequential, steps);
        return new RunPipelineStep(pipeline, ServiceProvider);
    }

    protected override PipelineStep CreateStepWithAction(Func<CancellationToken, Task> action)
    {
        var testStep = new TestStep(action, ServiceProvider);
        var pipeline = new TestPipeline(ServiceProvider, RunnerBehavior.Sequential, [testStep]);
        return new RunPipelineStep(pipeline, ServiceProvider);
    }

    protected override Type GetExpectedExceptionType(Exception thrownException)
    {
        if (thrownException is AggregateException aggregateException && aggregateException.IsExceptionType<OperationCanceledException>())
        {
            return aggregateException.InnerExceptions.FirstOrDefault(p => p.IsExceptionType<OperationCanceledException>())
                ?.InnerException is not null ? typeof(StepFailureException) : typeof(OperationCanceledException);
        }
        
        if (thrownException.IsExceptionType<OperationCanceledException>() || thrownException is StopRunnerException)
            return typeof(OperationCanceledException);
        return typeof(StepFailureException);
    }

    #region Constructor

    [Fact]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RunPipelineStep(null!, ServiceProvider));
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var pipeline = new TestPipeline(ServiceProvider, RunnerBehavior.Sequential, []);
        Assert.Throws<ArgumentNullException>(() => new RunPipelineStep(pipeline, null!));
    }

    [Fact]
    public void Constructor_ValidArguments_CreatesInstance()
    {
        var pipeline = new TestPipeline(ServiceProvider, RunnerBehavior.Sequential, []);

        var step = new RunPipelineStep(pipeline, ServiceProvider);

        Assert.NotNull(step);
        Assert.False(step.IsDisposed);
    }

    #endregion

    #region RunAsync

    [Fact]
    public async Task RunAsync_ExecutesPipelineSteps()
    {
        var step1Executed = false;
        var step2Executed = false;

        var testStep1 = new TestStep(_ =>
        {
            step1Executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        var testStep2 = new TestStep(_ =>
        {
            step2Executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = new TestPipeline(ServiceProvider, RunnerBehavior.Sequential, [testStep1, testStep2]);
        var runPipelineStep = new RunPipelineStep(pipeline, ServiceProvider);

        await runPipelineStep.RunAsync(CancellationToken.None);

        Assert.True(step1Executed);
        Assert.True(step2Executed);
    }

    [Fact]
    public async Task RunAsync_ExecutesStepsInOrder()
    {
        var executionOrder = new List<int>();

        var testStep1 = new TestStep(_ =>
        {
            executionOrder.Add(1);
            return Task.CompletedTask;
        }, ServiceProvider);

        var testStep2 = new TestStep(_ =>
        {
            executionOrder.Add(2);
            return Task.CompletedTask;
        }, ServiceProvider);

        var testStep3 = new TestStep(_ =>
        {
            executionOrder.Add(3);
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = new TestPipeline(ServiceProvider, RunnerBehavior.Sequential, [testStep1, testStep2, testStep3]);
        var runPipelineStep = new RunPipelineStep(pipeline, ServiceProvider);

        await runPipelineStep.RunAsync(CancellationToken.None);

        Assert.Equal([1,2,3], executionOrder);
    }

    [Fact]
    public async Task RunAsync_CancellationToken_IsPropagatedToSteps()
    {
        var tcs = new TaskCompletionSource<bool>();
        var stepRunning = new TaskCompletionSource<bool>();
        var cancellationObserved = false;

        var testStep = new TestStep(async ct =>
        {
            try
            {
                stepRunning.SetResult(true);
                await tcs.Task.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                cancellationObserved = true;
                throw;
            }
        }, ServiceProvider);

        var steps = new List<IStep> { testStep };
        var pipeline = new TestPipeline(ServiceProvider, RunnerBehavior.Sequential, steps);
        var runPipelineStep = new RunPipelineStep(pipeline, ServiceProvider);

        using var cts = new CancellationTokenSource();
        var runTask = runPipelineStep.RunAsync(cts.Token);

        await stepRunning.Task;
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => runTask);
        Assert.True(cancellationObserved);
    }

    [Fact]
    public async Task RunAsync_EmptyPipeline_CompletesSuccessfully()
    {
        var steps = new List<IStep>();
        var pipeline = new TestPipeline(ServiceProvider, RunnerBehavior.Sequential, steps);
        var runPipelineStep = new RunPipelineStep(pipeline, ServiceProvider);

        await runPipelineStep.RunAsync(CancellationToken.None);

        Assert.Null(runPipelineStep.Error);
    }

    [Fact]
    public async Task RunAsync_OnPrepareThrows_PropagatesException()
    {
        var expectedException = new InvalidOperationException("Prepare failed");
        var steps = new List<IStep>();
        var pipeline = new TestPipeline(
            ServiceProvider,
            RunnerBehavior.Sequential,
            steps,
            _ => throw expectedException);
        var runPipelineStep = new RunPipelineStep(pipeline, ServiceProvider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runPipelineStep.RunAsync(CancellationToken.None));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task RunAsync_PipelineThrowsStepFailedException()
    {
        var expectedException = new InvalidOperationException("Pipeline step failed");
        var step = new TestStep(_ => throw expectedException, ServiceProvider);
        var pipeline = new TestPipeline(
            ServiceProvider,
            RunnerBehavior.Sequential,
            [step]);
        
        var runPipelineStep = new RunPipelineStep(pipeline, ServiceProvider);

        var exception = await Assert.ThrowsAsync<StepFailureException>(
            () => runPipelineStep.RunAsync(CancellationToken.None));
        
        Assert.Contains("Pipeline step failed", exception.Message);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_DisposesPipeline()
    {
        var pipeline = new TestPipeline(
            ServiceProvider,
            RunnerBehavior.Sequential,
            new List<IStep>());
        var step = new RunPipelineStep(pipeline, ServiceProvider);

        step.Dispose();
        step.Dispose();
        step.Dispose();

        Assert.True(pipeline.IsDisposed);
        Assert.True(step.IsDisposed);
    }

    [Fact]
    public async Task Dispose_AfterRun_DisposesPipeline()
    {
        var pipeline = new TestPipeline(
            ServiceProvider,
            RunnerBehavior.Sequential,
            new List<IStep>());
        var step = new RunPipelineStep(pipeline, ServiceProvider);

        await step.RunAsync(CancellationToken.None);
        step.Dispose();

        Assert.True(step.IsDisposed);
    }

    #endregion

    private class TestPipeline(
        IServiceProvider serviceProvider,
        RunnerBehavior runnerBehavior,
        IList<IStep> steps,
        Func<CancellationToken, Task>? onPrepare = null) : StepRunnerPipeline(serviceProvider)
    {
        protected override IStepRunner CreateRunner()
        {
            if (runnerBehavior is RunnerBehavior.Sequential)
                return new SequentialStepRunner(ServiceProvider);
            return new AsyncStepRunner(4, ServiceProvider);
        }

        protected override async Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
        {
            if (onPrepare is not null)
                await onPrepare(token);
            return steps;
        }
    }
}