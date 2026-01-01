using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Extensions;
using AnakinRaW.CommonUtilities.Testing.Extensions;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public abstract class StepRunnerPipelineTestBase : PipelineTestBase
{
    /// <summary>
    /// Gets a value indicating steps are not executed in sequence of their addition to the underlying runner.
    /// </summary>
    protected virtual bool PipelineIsSequential => false;

    protected abstract StepRunnerPipelineBase CreateStepRunnerPipeline(IList<IStep> steps, bool failFast);

    protected override Pipeline CreatePipeline(IList<IStep> steps)
    {
        return CreateStepRunnerPipeline(steps, false);
    }

    #region FailFast

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailFast_CtorSetsProperty(bool failFast)
    {
        var pipeline = CreateStepRunnerPipeline([], failFast);
        Assert.Equal(failFast, pipeline.FailFast);
    }

    #endregion

    #region StepRunner

    [Fact]
    public void StepRunner_NotYetInitialized_ThrowsInvalidOperationException()
    {
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        var pipeline = CreateStepRunnerPipeline([step], Random.Bool());
        Assert.Throws<InvalidOperationException>(() => pipeline.StepRunner);
    }

    [Fact]
    public async Task StepRunner_IsNotNullAfterPreparation()
    {
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        var pipeline = CreateStepRunnerPipeline([step], Random.Bool());

        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(pipeline.StepRunner);
    }

    #endregion

    #region RunAsync

    [Fact]
    public async Task RunAsync_EmptyPipeline_Succeeds()
    {
        var pipeline = CreateStepRunnerPipeline([], true);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_EmptyPipeline_FailFastDisabled_Succeeds()
    {
        var pipeline = CreateStepRunnerPipeline([], false);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.False(pipeline.PipelineFailed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_AllStepsExecuted(bool prepare)
    {
        var runCounter = 0;

        var s1 = new TestStep(_ =>
        {
            Interlocked.Increment(ref runCounter);
            return Task.CompletedTask;
        }, ServiceProvider);
        var s2 = new TestStep(_ =>
        {
            Interlocked.Increment(ref runCounter);
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2], true);

        if (prepare)
        {
            await pipeline.PrepareAsync(TestContext.Current.CancellationToken);
        }

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, runCounter);
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_StepReceivesValidCancellationToken()
    {
        var receivedToken = CancellationToken.None;
        var tokenWasProvided = false;

        var step = new TestStep(ct =>
        {
            receivedToken = ct;
            tokenWasProvided = ct.CanBeCanceled;
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([step], true);

        using var cts = new CancellationTokenSource();
        await pipeline.RunAsync(cts.Token);

        Assert.True(tokenWasProvided);
        Assert.True(receivedToken.CanBeCanceled);
    }
    
    [Fact]
    public async Task RunAsync_FailFastEnabled_StepThrows_StopsExecution()
    {
        var secondStepRan = false;
        var s1 = new TestStep(_ => throw new Exception("Test"), ServiceProvider);
        var s2 = new TestStep(_ =>
        {
            secondStepRan = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2], true);

        await Assert.ThrowsAsync<StepFailureException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.PipelineFailed);

        if (PipelineIsSequential)
        {
            Assert.False(secondStepRan, "FailFast should prevent subsequent steps from running");
            Assert.True(pipeline.PipelineCancelled);
        }
    }

    [Fact]
    public async Task RunAsync_FailFastDisabled_ContinuesAfterError()
    {
        var executed = new List<int>();

        var s1 = new TestStep(_ => { executed.Add(1); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(_ => { executed.Add(2); throw new InvalidOperationException(); }, ServiceProvider);
        var s3 = new TestStep(_ => { executed.Add(3); return Task.CompletedTask; }, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2, s3], failFast: false);

        await Assert.ThrowsAsync<StepFailureException>(
            () => pipeline.RunAsync(TestContext.Current.CancellationToken));

        if (PipelineIsSequential)
            Assert.Equal([1, 2, 3], executed);
        else
            Assert.EqualUnordered([1, 2, 3], executed);
    }

    [Fact]
    public async Task RunAsync_StepThrowsException_ThrowsStepFailureException()
    {
        var step = new TestStep(_ => throw new InvalidOperationException("Test error"), ServiceProvider);
        var pipeline = CreatePipeline([step]);

        await Assert.ThrowsAsync<StepFailureException>(() =>
            pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.PipelineFailed);
        Assert.False(pipeline.PipelineCancelled);
    }

    [Fact]
    public async Task RunAsync_MultipleStepsThrow_AllErrorsCaptured()
    {
        var s1 = new TestStep(_ => throw new Exception("Error1"), ServiceProvider);
        var s2 = new TestStep(_ => throw new Exception("Error2"), ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2], false);

        var e = await Assert.ThrowsAsync<StepFailureException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.PipelineFailed);
        Assert.Contains("failed with error", e.Message);
        Assert.Contains("Error1", e.Message);
        Assert.Contains("Error2", e.Message);
    }

    [Fact]
    public async Task RunAsync_WithPreCancelledToken_ThrowsAndSetsPipelineCancelled()
    {
        var executed = false;
        var step = new TestStep(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);
        var pipeline = CreatePipeline([step]);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.RunAsync(cts.Token));

        Assert.False(executed);
        Assert.True(pipeline.PipelineCancelled);
        Assert.False(pipeline.PipelineFailed);
    }
    
    [Fact]
    public async Task RunAsync_StepThrowsOperationCanceledException_TreatedAsCancellation()
    {
        var s1 = new TestStep(_ => throw new OperationCanceledException(), ServiceProvider);

        var pipeline = CreatePipeline([s1]);

        // OperationCanceledException from a step should propagate
        await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.PipelineCancelled);
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_StepThrowsAggregateExceptionWithCancellation_SetsPipelineCancelled()
    {
        var step = new TestStep(_ =>
        {
            throw new AggregateException(new OperationCanceledException());
        }, ServiceProvider);

        var pipeline = CreatePipeline([step]);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.False(pipeline.PipelineFailed);
        Assert.True(pipeline.PipelineCancelled);
    }

    #endregion

    #region CreateRunner

    [Fact]
    public virtual async Task CreateRunner_ReturnsNull_DuringPrepare_ThrowsInvalidOperationException()
    {
        var pipeline = new NullRunnerPipeline(ServiceProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(async ()=> await pipeline.PrepareAsync(TestContext.Current.CancellationToken));

        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public virtual async Task CreateRunner_ReturnsNull_DuringRun_ThrowsInvalidOperationException()
    {
        var pipeline = new NullRunnerPipeline(ServiceProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.PipelineFailed);
    }

    #endregion

    private class NullRunnerPipeline(IServiceProvider serviceProvider) : StepRunnerPipelineBase(serviceProvider)
    {
        protected override IStepRunner CreateRunner()
        {
            return null!;
        }

        protected override Task PrepareRunnerAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}