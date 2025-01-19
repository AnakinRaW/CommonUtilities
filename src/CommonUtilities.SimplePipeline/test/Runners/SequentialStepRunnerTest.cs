using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using System;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class SequentialStepRunnerTest : StepRunnerTestBase<SequentialStepRunner>
{
    public override bool PreservesStepExecutionOrder => true;

    protected override SequentialStepRunner CreateStepRunner(bool deterministic = false)
    {
        return new SequentialStepRunner(ServiceProvider);
    }

    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SequentialStepRunner(null!));
    }

    [Fact]
    public async Task RunAsync_ErrorSetsCancellation()
    {
        var runner = CreateStepRunner();

        var errorCounter = 0;
        runner.Error += (_, e) =>
        {
            errorCounter++;
            if (e.Step.Error?.Message == "Test")
                e.Cancel = true;
        };

        var step1 = new TestStep(_ => throw new Exception("Test"), ServiceProvider);
        var ran2 = false;
        var step2 = new TestStep(_ => { ran2 = true; }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        var runnerTask = runner.RunAsync(CancellationToken.None);

        await runnerTask;

        Assert.Equal(2, errorCounter);
        Assert.False(ran2);
    }
}