using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sklavenwalker.CommonUtilities.TaskPipeline.Runners;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Sklavenwalker.CommonUtilities.TaskPipeline.Test.Runners;

public class ParallelBlockingRunnerTest
{
    [Fact]
    public void TestWaitNotFinished()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelBlockingRunner(2, sc.BuildServiceProvider());

        var t1 = new Mock<ITask>();
        var t2 = new Mock<ITask>();

        runner.Queue(t1.Object);
        runner.Queue(t2.Object);

        var runned1 = false;
        t1.Setup(t => t.Run(default)).Callback(() =>
        {
            runned1 = true;
        });
        var runned2 = false;
        t2.Setup(t => t.Run(default)).Callback(() =>
        {
            runned2 = true;
        });


        runner.Run(default);
        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromSeconds(2)));

        Assert.True(runned1);
        Assert.True(runned2);
    }

    [Fact]
    public void TestWaitFinished()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelBlockingRunner(2, sc.BuildServiceProvider());

        var t1 = new Mock<ITask>();
        var t2 = new Mock<ITask>();

        runner.Queue(t1.Object);
        runner.Queue(t2.Object);

        var runned1 = false;
        t1.Setup(t => t.Run(default)).Callback(() =>
        {
            runned1 = true;
        });
        var runned2 = false;
        t2.Setup(t => t.Run(default)).Callback(() =>
        {
            runned2 = true;
        });


        runner.Run(default);

        Task.Run(() =>
        {
            Task.Delay(1000);
            runner.Finish();
        });

        runner.Wait();

        Assert.True(runned1);
        Assert.True(runned2);
    }

    [Fact]
    public void TestWaitFinished2()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelBlockingRunner(2, sc.BuildServiceProvider());

        var t1 = new Mock<ITask>();
        var t2 = new Mock<ITask>();

        runner.Queue(t1.Object);
        runner.Queue(t2.Object);

        var runned1 = false;
        t1.Setup(t => t.Run(default)).Callback(() =>
        {
            runned1 = true;
        });
        var runned2 = false;
        t2.Setup(t => t.Run(default)).Callback(() =>
        {
            runned2 = true;
        });


        runner.Run(default);

        runner.FinishAndWait();

        Assert.True(runned1);
        Assert.True(runned2);
    }

    [Fact]
    public void TestAddDelayed()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelBlockingRunner(2, sc.BuildServiceProvider());

        var t1 = new Mock<ITask>();
        var t2 = new Mock<ITask>();
        var t3 = new Mock<ITask>();

        runner.Queue(t1.Object);
        runner.Queue(t2.Object);

        var runned1 = false;
        t1.Setup(t => t.Run(default)).Callback(() =>
        {
            runned1 = true;
        });
        var runned2 = false;
        t2.Setup(t => t.Run(default)).Callback(() =>
        {
            runned2 = true;
        });

        var runned3 = false;
        t3.Setup(t => t.Run(default)).Callback(() =>
        {
            runned3 = true;
        });


        runner.Run(default);

        Task.Run(() =>
        {
            runner.Queue(t3.Object);
            Task.Delay(1000);
            runner.Finish();
        });

        runner.Wait();

        Assert.True(runned1);
        Assert.True(runned2);
        Assert.True(runned3);
    }
}
