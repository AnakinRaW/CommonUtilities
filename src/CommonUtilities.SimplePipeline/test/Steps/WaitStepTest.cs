using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class WaitStepTest
{
    [Fact]
    public void Test_Wait()
    {
        var runner = new Mock<ISynchronizedStepRunner>();

        var sc = new ServiceCollection();
        var step = new WaitStep(runner.Object, sc.BuildServiceProvider());

        var flag = false;
        runner.Setup(r => r.Wait()).Callback(() =>
        {
            flag = true;
        });

        step.Run(default);
        Assert.True(flag);

    }
}