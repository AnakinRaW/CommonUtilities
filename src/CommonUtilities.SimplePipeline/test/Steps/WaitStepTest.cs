using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class WaitStepTest : CommonTestBase
{
    [Fact]
    public void Wait()
    {
        var runner = new ParallelStepRunner(2, ServiceProvider);

        var completed1 = false;
        var completed2 = false;
        runner.AddStep(new TestStep(_ =>
        {
            Task.Delay(1000, CancellationToken.None).Wait(CancellationToken.None);
            completed1 = true;
        }, ServiceProvider));
        runner.AddStep(new TestStep(_ =>
        {
            Task.Delay(1000, CancellationToken.None).Wait(CancellationToken.None);
            completed2 = true;
        }, ServiceProvider));

        var step = new WaitStep(runner, ServiceProvider);

        var runnerTask = runner.RunAsync(CancellationToken.None);
        step.Run(CancellationToken.None);

        Assert.True(runnerTask.IsCompleted);
        Assert.True(completed1);
        Assert.True(completed2);
    }
}