using System;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class ParallelBlockingRunnerTest
{
    [Fact]
    public void Test_Run_WaitNotFinished()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelBlockingRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        runner.AddStep(s1.Object);
        runner.AddStep(s2.Object);

        var ran1 = false;
        s1.Setup(t => t.Run(default)).Callback(() =>
        {
            ran1 = true;
        });
        var ran2 = false;
        s2.Setup(t => t.Run(default)).Callback(() =>
        {
            ran2 = true;
        });


        runner.Run(default);
        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromSeconds(2)));

        Assert.True(ran1);
        Assert.True(ran2);
    }

    [Fact]
    public void Test_Wait_Finished()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelBlockingRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        runner.AddStep(s1.Object);
        runner.AddStep(s2.Object);

        var ran1 = false;
        s1.Setup(t => t.Run(default)).Callback(() =>
        {
            ran1 = true;
        });
        var ran2 = false;
        s2.Setup(t => t.Run(default)).Callback(() =>
        {
            ran2 = true;
        });


        runner.Run(default);

        Task.Run(() =>
        {
            Task.Delay(1000);
            runner.Finish();
        });

        runner.Wait();

        Assert.True(ran1);
        Assert.True(ran2);
    }

    [Fact]
    public void Test_FinishAndWait()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelBlockingRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        runner.AddStep(s1.Object);
        runner.AddStep(s2.Object);

        var ran1 = false;
        s1.Setup(t => t.Run(default)).Callback(() =>
        {
            ran1 = true;
        });
        var ran2 = false;
        s2.Setup(t => t.Run(default)).Callback(() =>
        {
            ran2 = true;
        });


        runner.Run(default);

        runner.FinishAndWait();

        Assert.True(ran1);
        Assert.True(ran2);
    }

    [Fact]
    public void Test_Run_AddDelayed()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelBlockingRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();
        var s3 = new Mock<IStep>();

        runner.AddStep(s1.Object);
        runner.AddStep(s2.Object);

        var ran1 = false;
        s1.Setup(t => t.Run(default)).Callback(() =>
        {
            ran1 = true;
        });
        var ran2 = false;
        s2.Setup(t => t.Run(default)).Callback(() =>
        {
            ran2 = true;
        });

        var ran3 = false;
        s3.Setup(t => t.Run(default)).Callback(() =>
        {
            ran3 = true;
        });


        runner.Run(default);

        Task.Run(() =>
        {
            runner.AddStep(s3.Object);
            Task.Delay(1000);
            runner.Finish();
        });

        runner.Wait();

        Assert.True(ran1);
        Assert.True(ran2);
        Assert.True(ran3);
    }
}