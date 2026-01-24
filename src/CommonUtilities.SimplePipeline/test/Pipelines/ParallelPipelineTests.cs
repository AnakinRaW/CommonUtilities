using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using System;
using System.Collections.Generic;
using System.Linq;
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

    protected override StepRunnerPipeline CreateTrackingStepRunnerPipeline(
        IList<IStep> steps, bool failFast, RunnerBehavior runnerBehavior, List<string> callOrder,
        string? throwOnMethod = null, Func<IEnumerable<IStep>, IEnumerable<IStep>>? filterErrorStepsFunc = null)
    {
        return new TrackingParallelPipeline(ServiceProvider, steps, GetWorkerCount(runnerBehavior), failFast, callOrder,
            throwOnMethod, filterErrorStepsFunc);
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
        private readonly IList<IStep> _steps;
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
            _steps = steps.ToList();
            _workerCount = workerCount;
            FailFast = failFast;
            _prepareAction = onPrepare;
        }

        protected override IStepRunner CreateRunner()
        {
            return new AsyncStepRunner(_workerCount, ServiceProvider);
        }

        protected override async Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
        {
            if (_prepareAction is not null)
                await _prepareAction(token);
            return _steps;
        }
    }

    private class TrackingParallelPipeline : StepRunnerPipeline
    {
        private readonly IList<IStep> _steps;
        private readonly int _workerCount;
        private readonly Func<IEnumerable<IStep>, IEnumerable<IStep>>? _filterErrorStepsFunc;
        private readonly TrackingPipelineHelper _helper;

        public TrackingParallelPipeline(
            IServiceProvider serviceProvider,
            IList<IStep> steps,
            int workerCount,
            bool failFast,
            List<string> callOrder,
            string? throwOnMethod,
            Func<IEnumerable<IStep>, IEnumerable<IStep>>? filterErrorStepsFunc)
            : base(serviceProvider)
        {
            _steps = steps;
            _workerCount = workerCount;
            _filterErrorStepsFunc = filterErrorStepsFunc;
            _helper = new TrackingPipelineHelper(callOrder, throwOnMethod);
            FailFast = failFast;
        }

        protected override IEnumerable<IStep> GetFailedSteps(IEnumerable<IStep> steps)
        {
            return _filterErrorStepsFunc is null ? base.GetFailedSteps(steps) : _filterErrorStepsFunc(steps);
        }

        protected override IStepRunner CreateRunner()
        {
            return new AsyncStepRunner(_workerCount, ServiceProvider);
        }

        protected override Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
        {
            return Task.FromResult(_steps);
        }

        protected override void OnExecuteStarted() => _helper.OnExecuteStarted();
        protected override void OnRunnerExecuted() => _helper.OnRunnerExecuted();
        protected override void OnExecuteCompleted() => _helper.OnExecuteCompleted();
    }
}