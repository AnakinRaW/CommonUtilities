using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public abstract class StepRunnerPipelineTestBase : StepRunnerPipelineBaseTestBase<IStepRunner>
{
    protected abstract StepRunnerPipeline CreateStepRunnerPipeline(IList<IStep> steps, bool failFast, RunnerBehavior runnerBehavior);

    protected override StepRunnerPipelineBase<IStepRunner> CreateStepRunnerPipelineBase(IList<IStep> steps, bool failFast, RunnerBehavior runnerBehavior)
    {
        return CreateStepRunnerPipeline(steps, failFast, runnerBehavior);
    }

    #region CreateRunner

    [Fact]
    public virtual async Task CreateRunner_ReturnsNull_DuringPrepare_ThrowsInvalidOperationException()
    {
        var pipeline = new NullRunnerPipeline(ServiceProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(async ()=> await pipeline.PrepareAsync(TestContext.Current.CancellationToken));

        Assert.False(pipeline.Failed);
    }

    [Fact]
    public virtual async Task CreateRunner_ReturnsNull_DuringRun_ThrowsInvalidOperationException()
    {
        var pipeline = new NullRunnerPipeline(ServiceProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.Failed);
    }

    #endregion

    private class NullRunnerPipeline(IServiceProvider serviceProvider) : StepRunnerPipeline(serviceProvider)
    {
        protected override IStepRunner CreateRunner()
        {
            return null!;
        }

        protected override Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
        {
            return Task.FromResult<IList<IStep>>([]);
        }
    }
}