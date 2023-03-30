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
        var runner = new ParallelTaskRunner(2, sc.BuildServiceProvider());

        var t1 = new Mock<ITask>();
        var t2 = new Mock<ITask>();

        runner.Queue(t1.Object);
        runner.Queue(t2.Object);

        var runned1 = false;
        t1.Setup(t => t.Run(default)).Callback(() =>
        {
            Task.Delay(1000);
            runned1 = true;
        });
        var runned2 = false;
        t2.Setup(t => t.Run(default)).Callback(() =>
        {
            Task.Delay(1000);
            runned2 = true;
        });


        runner.Run(default);
        runner.Wait();

        Assert.True(runned1);
        Assert.True(runned2);
    }

    [Fact]
    public void TestNoWait()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelTaskRunner(2, sc.BuildServiceProvider());

        var t1 = new Mock<ITask>();
        var t2 = new Mock<ITask>();

        var b = new ManualResetEvent(false);

        runner.Queue(t1.Object);
        runner.Queue(t2.Object);

        var runned1 = false;
        t1.Setup(t => t.Run(default)).Callback(() =>
        {
            b.WaitOne();
            runned1 = true;
        });
        var runned2 = false;
        t2.Setup(t => t.Run(default)).Callback(() =>
        {
            b.WaitOne();
            runned2 = true;
        });


        runner.Run(default);
        Assert.False(runned1);
        Assert.False(runned2);
        b.Set();
    }

    [Fact]
    public void TestWaitTimeout()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelTaskRunner(2, sc.BuildServiceProvider());

        var t1 = new Mock<ITask>();

        var b = new ManualResetEvent(false);

        runner.Queue(t1.Object);

        var runned1 = false;
        t1.Setup(t => t.Run(default)).Callback(() =>
        {
            b.WaitOne();
            runned1 = true;
        });

        runner.Run(default);
        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromMilliseconds(100)));
        b.Set();
    }

    [Fact]
    public void TestRunWithError()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelTaskRunner(2, sc.BuildServiceProvider());

        var hasError = false;
        runner.Error += (_, __) =>
        {
            hasError = true;
        };

        var task = new Mock<ITask>();
        var runned = false;
        task.Setup(t => t.Run(default)).Callback(() =>
        {
            runned = true;
        }).Throws<Exception>();

        runner.Queue(task.Object);
        runner.Run(default);
        runner.Wait(Timeout.InfiniteTimeSpan);

        Assert.True(hasError);
        Assert.True(runned);
        Assert.NotNull(runner.Exception);
    }

    [Fact]
    public void TestRunCancelled()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelTaskRunner(1, sc.BuildServiceProvider());

        var cts = new CancellationTokenSource();

        var b = new ManualResetEvent(false);

        var hasError = false;
        runner.Error += (_, __) =>
        {
            hasError = true;
        };

        var t1 = new Mock<ITask>();
        var runned = false;
        t1.Setup(t => t.Run(cts.Token)).Callback(() =>
        {
            runned = true;
            cts.Cancel();
            b.Set();
        });
        var t2 = new Mock<ITask>();
        t2.Setup(t => t.Run(cts.Token)).Callback(() =>
        {
            b.WaitOne();
        });

        runner.Queue(t1.Object);
        runner.Queue(t2.Object);
        runner.Run(cts.Token);
        runner.Wait(Timeout.InfiniteTimeSpan);

        Assert.True(hasError);
        Assert.True(runned);
        Assert.True(runner.IsCancelled);
    }
}
