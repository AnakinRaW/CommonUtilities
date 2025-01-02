using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class ParallelStepRunnerTest
{
    [Fact]
    public void Test_Wait()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelStepRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        runner.AddStep(s1.Object);
        runner.AddStep(s2.Object);

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


        _ = runner.RunAsync(default);
        runner.Wait();

        Assert.True(ran1);
        Assert.True(ran2);
    }

    [Fact]
    public async Task Test_Run_NoWait()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelStepRunner(2, sc.BuildServiceProvider());


        var b = new ManualResetEvent(false);

        var ran1 = false;
        var s1 = new TestStep(_ =>
        {
            b.WaitOne();
            ran1 = true;
        });

        var ran2 = false;
        var s2 = new TestStep(_ =>
        {
            b.WaitOne();
            ran2 = true;
        });

        runner.AddStep(s1);
        runner.AddStep(s2);


        var runTask = runner.RunAsync(default);
        Assert.False(ran1);
        Assert.False(ran2);
        b.Set();

        await runTask;

        Assert.True(ran1);
        Assert.True(ran2);

        runner.Dispose();

        Assert.True(s1.IsDisposed);
        Assert.True(s2.IsDisposed);

        Assert.Equal([s1, s2], runner.Steps);
    }

    [Fact]
    public async Task Test_Wait_Timeout_ThrowsTimeoutException()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelStepRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();

        var b = new ManualResetEvent(false);

        runner.AddStep(s1.Object);

        s1.Setup(t => t.Run(default)).Callback(() =>
        {
            b.WaitOne();
        });

        var runnerTask = runner.RunAsync(default);

        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromMilliseconds(100)));
        b.Set();

        await runnerTask;
    }

    [Fact]
    public async Task Test_Run_WithError()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelStepRunner(2, sc.BuildServiceProvider());

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

        runner.AddStep(step.Object);
        await runner.RunAsync(default);

        Assert.True(hasError);
        Assert.True(ran);
        Assert.NotNull(runner.Exception);
    }

    [Fact]
    public async Task Test_Run_Cancelled()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelStepRunner(1, sc.BuildServiceProvider());

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

        runner.AddStep(t1.Object);
        runner.AddStep(t2.Object);
        await runner.RunAsync(cts.Token);

        Assert.True(hasError);
        Assert.True(ran);
        Assert.True(runner.IsCancelled);
    }
}