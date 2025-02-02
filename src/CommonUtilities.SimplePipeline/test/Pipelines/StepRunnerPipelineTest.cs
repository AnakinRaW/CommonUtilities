using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public abstract class StepRunnerPipelineTest<T> : PipelineTest where T : IStepRunner
{
    protected abstract StepRunnerPipeline<T> CreatePipeline(IList<IStep> steps, bool failFast);

    [Fact]
    public async Task RunAsync_EmptyPipeline()
    {
        var pipeline = CreatePipeline([], true);

        await pipeline.RunAsync();
        Assert.False(pipeline.PipelineFailed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_AllStepsExecuted(bool prepare)
    {
        var runCounter = 0;

        var s1 = new TestStep(_ => Interlocked.Increment(ref runCounter), ServiceProvider);
        var s2 = new TestStep(_ => Interlocked.Increment(ref runCounter), ServiceProvider);

        var pipeline = CreatePipeline([s1, s2], true);

        if (prepare)
        {
            await pipeline.PrepareAsync();
            await pipeline.PrepareAsync(); // Double prepare should have no effect
        }

        await pipeline.RunAsync();
        Assert.Equal(2, runCounter);
        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_WithError_Throws()
    {
        var s1 = new TestStep(_ => throw new Exception("Test"), ServiceProvider);

        var pipeline = CreatePipeline([s1], true);

        var e = await Assert.ThrowsAsync<StepFailureException>(async () => await pipeline.RunAsync());
        Assert.Equal("Step 'TestStep' failed with error: Test", e.Message);
        Assert.True(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_WithError_FailSlow_Throws()
    {
        var ran = false;
        var s1 = new TestStep(_ => throw new Exception("Test"), ServiceProvider);
        var s2 = new TestStep(_ => ran = true, ServiceProvider);

        var pipeline = CreatePipeline([s1, s2], false);

        var e = await Assert.ThrowsAsync<StepFailureException>(async () => await pipeline.RunAsync());
        Assert.Equal("Step 'TestStep' failed with error: Test", e.Message);
        Assert.True(pipeline.PipelineFailed);
        Assert.True(ran);
    }

    [Fact]
    public async Task PrepareAsync_ReturnsNull_Throws()
    {
        var pipeline = new NullRunnerPipeline(ServiceProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(pipeline.PrepareAsync);

        // Should not throw, as preparation is only done once.
        await pipeline.PrepareAsync();

        Assert.False(pipeline.PipelineFailed);
    }

    private class NullRunnerPipeline(IServiceProvider serviceProvider) : StepRunnerPipeline<T>(serviceProvider)
    {
        protected override T CreateRunner()
        {
            return default!;
        }

        protected override Task<IList<IStep>> BuildSteps()
        {
            return Task.FromResult<IList<IStep>>([]);
        }
    }
}