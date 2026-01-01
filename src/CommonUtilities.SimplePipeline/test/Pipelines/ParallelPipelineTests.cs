using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class ParallelPipelineTests : StepRunnerPipelineTestBase
{
    protected override ITrackingPipeline CreateTrackingPipeline(Func<CancellationToken, Task> prepare, Func<CancellationToken, Task> run)
    {
        var testStep = new TestStep(run, ServiceProvider);
        return new TestParallelPipeline(ServiceProvider, [testStep], prepare, GetWorkerCount(GetRandomRunBehavior()), false);
    }

    protected override StepRunnerPipeline CreateStepRunnerPipeline(IList<IStep> steps, bool failFast, RunnerBehavior runnerBehavior)
    {
        return new TestParallelPipeline(ServiceProvider, steps, null, GetWorkerCount(runnerBehavior), failFast);
    }

    #region Constructor Tests

    [Fact]
    public void Ctor_NullServiceProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestParallelPipeline(null!, [], null));
    }

    #endregion

    private class TestParallelPipeline : StepRunnerPipeline, ITrackingPipeline
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