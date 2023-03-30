using AnakinRaW.CommonUtilities.SimplePipeline.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Tasks;

public class WaitTastTest
{
    [Fact]
    public void TestWait()
    {
        var runner = new Mock<IParallelRunner>();
        var sc = new ServiceCollection();
        var task = new WaitTask(runner.Object, sc.BuildServiceProvider());

        var flag = false;
        runner.Setup(r => r.Wait()).Callback(() =>
        {
            flag = true;
        });

        task.Run(default);
        Assert.True(flag);

    }
}