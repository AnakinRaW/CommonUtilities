using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class SequentialStepRunnerTest : StepRunnerTestBase<StepRunner>
{
    public override bool PreservesStepExecutionOrder => true;

    protected override StepRunner CreateStepRunner()
    {
        return new StepRunner(ServiceProvider);
    }

    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new StepRunner(null!));
    }
}