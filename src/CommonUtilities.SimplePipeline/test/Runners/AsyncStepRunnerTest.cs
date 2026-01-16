using AnakinRaW.CommonUtilities.SimplePipeline.Runners;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class AsyncStepRunnerTest : StepRunnerTestBase<AsyncStepRunner>
{
    public override bool HasSequentialStepExecutionOrder => false;

    public override bool SupportsSequentialExecutionOrder => true;

    protected override AsyncStepRunner CreateStepRunner(bool? sequential = null)
    {
        var workers = sequential is true ? 1 : 4;
        return CreateStepRunner(workers);
    }

    protected override AsyncStepRunner CreateStepRunner(int workerCount)
    {
        return new AsyncStepRunner(workerCount, ServiceProvider);
    }
}