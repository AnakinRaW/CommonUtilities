using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Extensions;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class SequentialPipelineTests : StepRunnerPipelineTestBase
{
    protected override bool RunnerSupportsConcurrentRuns => false;

    protected override StepRunnerPipeline CreateStepRunnerPipeline(IList<IStep> steps, bool failFast, RunnerBehavior runnerBehavior)
    {
        if (runnerBehavior is RunnerBehavior.Concurrent)
            throw new NotSupportedException("Concurrent runs are not supported");
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

        protected override async Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
        {
            if (_prepareAction is not null)
                await _prepareAction(token);
            return _steps;
        }
    }
}