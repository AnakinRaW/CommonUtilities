using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Extensions;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class SequentialPipelineTests : StepRunnerPipelineTestBase
{
    protected override bool PipelineIsSequential => true;

    protected override StepRunnerPipelineBase CreateStepRunnerPipeline(IList<IStep> steps, bool failFast)
    {
        return CreateSequentialPipeline(steps, failFast);
    }

    protected override ITrackingPipeline CreateTrackingPipeline(Func<CancellationToken, Task> prepare, Func<CancellationToken, Task> run)
    {
        var testStep = new TestStep(run, ServiceProvider);
        return new TestSequentialPipeline(ServiceProvider, [testStep], prepare, failFast: false);
    }

    private SequentialPipeline CreateSequentialPipeline(IList<IStep> steps, bool failFast)
    {
        return new TestSequentialPipeline(ServiceProvider, steps, null, failFast);
    }

    #region Constructor Tests

    [Fact]
    public void Ctor_NullServiceProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestSequentialPipeline(null!, [], null, Random.Bool()));
    }

    #endregion

    #region Sequential Execution Tests

    [Fact]
    public async Task RunAsync_MultipleSteps_ExecutesInSequence()
    {
        var sb = new StringBuilder();

        var s1 = new TestStep(_ => { sb.Append('a'); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(_ => { sb.Append('b'); return Task.CompletedTask; }, ServiceProvider);
        var s3 = new TestStep(_ => { sb.Append('c'); return Task.CompletedTask; }, ServiceProvider);

        var pipeline = CreatePipeline([s1, s2, s3]);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal("abc", sb.ToString());
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_WithAsyncSteps_MaintainsOrder()
    {
        var sb = new StringBuilder();

        var s1 = new TestStep(async _ =>
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
            sb.Append('a');
        }, ServiceProvider);
        var s2 = new TestStep(async _ =>
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
            sb.Append('b');
        }, ServiceProvider);
        var s3 = new TestStep(_ =>
        {
            sb.Append('c');
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = CreatePipeline([s1, s2, s3]);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal("abc", sb.ToString());
    }

    #endregion

    #region Error Handling Tests

    [Theory]
    [InlineData(true, "a")]
    [InlineData(false, "ac")]
    public async Task RunAsync_StepThrows_FailFastBehavior(bool failFast, string expectedResult)
    {
        var sb = new StringBuilder();

        var s1 = new TestStep(_ => { sb.Append('a'); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(_ => throw new InvalidOperationException("Test"), ServiceProvider);
        var s3 = new TestStep(_ => { sb.Append('c'); return Task.CompletedTask; }, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2, s3], failFast);

        var ex = await Assert.ThrowsAsync<StepFailureException>(
            () => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.Equal("Step 'TestStep' failed with error: Test", ex.Message);
        Assert.Equal(expectedResult, sb.ToString());
        Assert.True(pipeline.PipelineFailed);
        Assert.Equal(failFast, pipeline.PipelineCancelled);
    }

    [Theory]
    [InlineData(true, "")]
    [InlineData(false, "b")]
    public async Task RunAsync_FirstStepThrows_FailFastBehavior(bool failFast, string expectedResult)
    {
        var sb = new StringBuilder();

        var s1 = new TestStep(_ => throw new InvalidOperationException("Test"), ServiceProvider);
        var s2 = new TestStep(_ => { sb.Append('b'); return Task.CompletedTask; }, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2], failFast);

        await Assert.ThrowsAsync<StepFailureException>(
            () => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.Equal(expectedResult, sb.ToString());
        Assert.True(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_LastStepThrows_SetsPipelineFailed()
    {
        var sb = new StringBuilder();

        var s1 = new TestStep(_ => { sb.Append('a'); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(_ => throw new InvalidOperationException("Test"), ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2], false);

        await Assert.ThrowsAsync<StepFailureException>(
            () => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.Equal("a", sb.ToString());
        Assert.True(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_MultipleStepsFail_ThrowsWithAllFailures()
    {
        var s1 = new TestStep(_ => throw new InvalidOperationException("Error1"), ServiceProvider);
        var s2 = new TestStep(_ => throw new InvalidOperationException("Error2"), ServiceProvider);
        var s3 = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2, s3], false);

        var ex = await Assert.ThrowsAsync<StepFailureException>(
            () => pipeline.RunAsync(TestContext.Current.CancellationToken));

        // TODO:
        //Assert.Equal(2, ex.FailedSteps.Count());
        Assert.True(pipeline.PipelineFailed);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task RunAsync_CancelledMidSequence_StopsOnFailFast()
    {
        using var cts = new CancellationTokenSource();
        var executedSteps = new List<int>();

        var s1 = new TestStep(_ => { executedSteps.Add(1); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(_ =>
        {
            executedSteps.Add(2);
            cts.Cancel();
            return Task.CompletedTask;
        }, ServiceProvider);
        var s3 = new TestStep(_ => { executedSteps.Add(3); return Task.CompletedTask; }, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2, s3], true);

        await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.RunAsync(cts.Token));

        Assert.Equal([1, 2], executedSteps);
        Assert.True(pipeline.PipelineCancelled);
    }

    #endregion

    #region FailFast Tests

    [Fact]
    public async Task FailFast_True_StopsOnFirstError()
    {
        var executed = new List<int>();

        var s1 = new TestStep(_ => { executed.Add(1); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(_ => { executed.Add(2); throw new InvalidOperationException(); }, ServiceProvider);
        var s3 = new TestStep(_ => { executed.Add(3); return Task.CompletedTask; }, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2, s3], failFast: true);

        await Assert.ThrowsAsync<StepFailureException>(
            () => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.Equal([1, 2], executed);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task RunAsync_SingleStep_ExecutesCorrectly()
    {
        var executed = false;
        var step = new TestStep(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);
        var pipeline = CreatePipeline([step]);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(executed);
        Assert.False(pipeline.PipelineFailed);
    }

    #endregion

    private class TestSequentialPipeline : SequentialPipeline, ITrackingPipeline
    {
        private readonly IList<IStep> _steps;
        private readonly Func<CancellationToken, Task>? _prepareAction;

        public TestSequentialPipeline(
            IServiceProvider serviceProvider,
            IList<IStep> steps,
            Func<CancellationToken, Task>? onPrepare,
            bool failFast = false)
            : base(serviceProvider)
        {
            _steps = steps;
            FailFast = failFast;
            _prepareAction = onPrepare;
        }

        protected override Task PrepareRunnerAsync(CancellationToken token)
        {
            foreach (var step in _steps) 
                StepRunner.AddStep(step);

            return _prepareAction is null ? Task.CompletedTask : _prepareAction(token);
        }
    }
}