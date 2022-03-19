using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sklavenwalker.CommonUtilities.TaskPipeline.Tasks;
using Xunit;

namespace Sklavenwalker.CommonUtilities.TaskPipeline.Test.Tasks;

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