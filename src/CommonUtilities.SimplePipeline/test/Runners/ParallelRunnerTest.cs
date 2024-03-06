using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class ParallelRunnerTest
{
    [Fact]
    public void TestWait()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        runner.Queue(s1.Object);
        runner.Queue(s2.Object);

        var ran1 = false;
        s1.Setup(t => t.Run(default)).Callback(() =>
        {
            Task.Delay(1000);
            ran1 = true;
        });
        var ran2 = false;
        s2.Setup(t => t.Run(default)).Callback(() =>
        {
            Task.Delay(1000);
            ran2 = true;
        });


        runner.Run(default);
        runner.Wait();

        Assert.True(ran1);
        Assert.True(ran2);
    }

    [Fact]
    public void TestNoWait()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        var b = new ManualResetEvent(false);

        runner.Queue(s1.Object);
        runner.Queue(s2.Object);

        var ran1 = false;
        s1.Setup(t => t.Run(default)).Callback(() =>
        {
            b.WaitOne();
            ran1 = true;
        });
        var ran2 = false;
        s2.Setup(t => t.Run(default)).Callback(() =>
        {
            b.WaitOne();
            ran2 = true;
        });


        runner.Run(default);
        Assert.False(ran1);
        Assert.False(ran2);
        b.Set();
    }

    [Fact]
    public void TestWaitTimeout()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();

        var b = new ManualResetEvent(false);

        runner.Queue(s1.Object);

        s1.Setup(t => t.Run(default)).Callback(() =>
        {
            b.WaitOne();
        });

        runner.Run(default);
        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromMilliseconds(100)));
        b.Set();
    }

    [Fact]
    public void TestRunWithError()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelRunner(2, sc.BuildServiceProvider());

        var hasError = false;
        runner.Error += (_, _) =>
        {
            hasError = true;
        };

        var step = new Mock<IStep>();
        var ran = false;
        step.Setup(t => t.Run(default)).Callback(() =>
        {
            ran = true;
        }).Throws<Exception>();

        runner.Queue(step.Object);
        runner.Run(default);
        runner.Wait(Timeout.InfiniteTimeSpan);

        Assert.True(hasError);
        Assert.True(ran);
        Assert.NotNull(runner.Exception);
    }

    [Fact]
    public void TestRunCancelled()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelRunner(1, sc.BuildServiceProvider());

        var cts = new CancellationTokenSource();

        var b = new ManualResetEvent(false);

        var hasError = false;
        runner.Error += (_, _) =>
        {
            hasError = true;
        };

        var t1 = new Mock<IStep>();
        var ran = false;
        t1.Setup(t => t.Run(cts.Token)).Callback(() =>
        {
            ran = true;
            cts.Cancel();
            b.Set();
        });
        var t2 = new Mock<IStep>();
        t2.Setup(t => t.Run(cts.Token)).Callback(() =>
        {
            b.WaitOne();
        });

        runner.Queue(t1.Object);
        runner.Queue(t2.Object);
        runner.Run(cts.Token);
        runner.Wait(Timeout.InfiniteTimeSpan);

        Assert.True(hasError);
        Assert.True(ran);
        Assert.True(runner.IsCancelled);
    }
}
