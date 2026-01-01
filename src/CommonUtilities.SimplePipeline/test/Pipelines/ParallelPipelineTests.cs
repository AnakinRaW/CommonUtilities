using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class ParallelPipelineTests : StepRunnerPipelineTestBase
{
    protected override ITrackingPipeline CreateTrackingPipeline(Func<CancellationToken, Task> prepare, Func<CancellationToken, Task> run)
    {
        var testStep = new TestStep(run, ServiceProvider);
        return new TestParallelPipeline(ServiceProvider, [testStep], prepare, 4, false);
    }

    protected override StepRunnerPipelineBase CreateStepRunnerPipeline(IList<IStep> steps, bool failFast)
    {
        return new TestParallelPipeline(ServiceProvider, steps, null, failFast: failFast);
    }

    #region Constructor Tests

    [Fact]
    public void Ctor_NullServiceProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestParallelPipeline(null!, [], null));
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task RunAsync_ExecutesStepsInParallel()
    {
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        var steps = new List<IStep>();
        for (var i = 0; i < 4; i++)
        {
            steps.Add(new TestStep(async _ =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }

                await Task.Delay(100, TestContext.Current.CancellationToken);

                lock (lockObj)
                {
                    concurrentCount--;
                }
            }, ServiceProvider));
        }

        var pipeline = new TestParallelPipeline(ServiceProvider, steps, null, workerCount: 4);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(maxConcurrent > 1, $"Expected parallel execution, but max concurrent was {maxConcurrent}");
    }

    [Fact]
    public async Task RunAsync_RespectsWorkerCount()
    {
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();
        const int workerCount = 2;

        var steps = new List<IStep>();
        for (var i = 0; i < 6; i++)
        {
            steps.Add(new TestStep(async _ =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }

                await Task.Delay(100, TestContext.Current.CancellationToken);

                lock (lockObj)
                {
                    concurrentCount--;
                }
            }, ServiceProvider));
        }

        var pipeline = new TestParallelPipeline(ServiceProvider, steps, null, workerCount: workerCount);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(maxConcurrent <= workerCount, $"Expected max {workerCount} concurrent, but was {maxConcurrent}");
    }

    [Fact]
    public async Task RunAsync_AllStepsExecuteEvenWithVaryingDurations()
    {
        var executedSteps = new ConcurrentBag<int>();

        var s1 = new TestStep(async _ =>
        {
            await Task.Delay(150, TestContext.Current.CancellationToken);
            executedSteps.Add(1);
        }, ServiceProvider);
        var s2 = new TestStep(async _ =>
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
            executedSteps.Add(2);
        }, ServiceProvider);
        var s3 = new TestStep(async _ =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            executedSteps.Add(3);
        }, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2, s3], true);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, executedSteps.Count);
        Assert.Contains(1, executedSteps);
        Assert.Contains(2, executedSteps);
        Assert.Contains(3, executedSteps);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task RunAsync_MultipleErrors_FailFastDisabled_CollectsAllErrors()
    {
        var s1 = new TestStep(_ => throw new Exception("Error1"), ServiceProvider);
        var s2 = new TestStep(_ => throw new Exception("Error2"), ServiceProvider);
        var s3 = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        var pipeline = CreateStepRunnerPipeline([s1, s2, s3], false);

        var e = await Assert.ThrowsAsync<StepFailureException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.PipelineFailed);
        // Both errors should be captured
        Assert.NotNull(s1.Error);
        Assert.NotNull(s2.Error);
        Assert.Null(s3.Error);
    }

    #endregion

    private class TestParallelPipeline : StepRunnerPipelineBase, ITrackingPipeline
    {
        private readonly IEnumerable<IStep> _steps;
        private readonly int _workerCount;
        private readonly Func<CancellationToken, Task>? _prepareAction;

        public TestParallelPipeline(
            IServiceProvider serviceProvider,
            IEnumerable<IStep> steps, 
            Func<CancellationToken, Task>? onPrepare,
            int workerCount = 4,
            bool failFast = true) 
            : base(serviceProvider)
        {
            _steps = steps;
            _workerCount = workerCount;
            FailFast = failFast;
            _prepareAction = onPrepare;
        }

        protected override IStepRunner CreateRunner()
        {
            return new AsyncStepRunner(_workerCount, ServiceProvider);
        }

        protected override Task PrepareRunnerAsync(CancellationToken token)
        {
            foreach (var step in _steps) 
                StepRunner.AddStep(step);

            return _prepareAction is null ? Task.CompletedTask : _prepareAction(token);
        }
    }
}