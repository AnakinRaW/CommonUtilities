using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class SequentialStepRunnerTest : StepRunnerTestSuite<SequentialStepRunner>
{
    public override bool HasSequentialStepExecutionOrder => true;

    public override bool SupportsSequentialExecutionOrder => true;

    protected override SequentialStepRunner CreateStepRunner(bool? sequential = null)
    {
        if (sequential is false)
            throw new InvalidOperationException();
        return new SequentialStepRunner(ServiceProvider);
    }

    protected override SequentialStepRunner CreateStepRunner(int workerCount)
    {
        throw new NotSupportedException();
    }

    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SequentialStepRunner(null!));
    }

    [Fact]
    public void Ctor_WorkerCountIsOne()
    {
        var runner = new SequentialStepRunner(ServiceProvider);
        Assert.Equal(1, runner.WorkerCount);
    }
}