using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class ParallelPipelineTests : StepRunnerPipelineTest<ParallelStepRunner>
{
    protected override Pipeline CreatePipeline(IList<IStep> steps)
    {
        return CreatePipeline(steps, true);
    }

    protected override StepRunnerPipeline<ParallelStepRunner> CreatePipeline(IList<IStep> steps, bool failFast)
    {
        return new TestParallelPipeline(steps, ServiceProvider, failFast: failFast);
    }
    
    private class TestParallelPipeline(IList<IStep> steps, IServiceProvider serviceProvider, int workerCount = 4, bool failFast = true)
        : ParallelPipeline(serviceProvider, workerCount, failFast)
    {
        protected override Task<IList<IStep>> BuildSteps()
        {
            return Task.FromResult(steps);
        }
    }
}