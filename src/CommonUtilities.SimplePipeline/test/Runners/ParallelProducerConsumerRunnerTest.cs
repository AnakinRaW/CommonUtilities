using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class ParallelProducerConsumerRunnerTest
{
    [Fact]
    public async void Test_Run_WaitNotFinished()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelProducerConsumerRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        runner.AddStep(s1.Object);
        runner.AddStep(s2.Object);

        var tsc1 = new TaskCompletionSource<int>();
        var tsc2 = new TaskCompletionSource<int>();

        s1.Setup(t => t.Run(default)).Callback(() =>
        {
            tsc1.SetResult(1);
        });
        s2.Setup(t => t.Run(default)).Callback(() =>
        {
            tsc2.SetResult(1);
        });
        
        
        _ = runner.RunAsync(default);

        await tsc1.Task;
        await tsc1.Task;

        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task Test_Run_AwaitDoesNotThrow()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelProducerConsumerRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        runner.AddStep(s1.Object);
        runner.AddStep(s2.Object);

        runner.Finish();

        s1.Setup(t => t.Run(default)).Throws<Exception>();

        var ran2 = false;
        s2.Setup(t => t.Run(default)).Callback(() =>
        {
            ran2 = true;
        });


        await runner.RunAsync(default);
        Assert.NotNull(runner.Exception);
        Assert.True(ran2);

        Assert.Equal([s1.Object, s2.Object], new HashSet<IStep>(runner.Steps));
    }

    [Fact]
    public void Test_Run_SyncWaitDoesThrow()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelProducerConsumerRunner(2, sc.BuildServiceProvider());

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        runner.AddStep(s1.Object);
        runner.AddStep(s2.Object);

        runner.Finish();

        s1.Setup(t => t.Run(default)).Throws<Exception>();

        var ran2 = false;
        s2.Setup(t => t.Run(default)).Callback(() =>
        {
            ran2 = true;
        });


        runner.RunAsync(default);

        Assert.Throws<AggregateException>(() => runner.Wait());

        Assert.NotNull(runner.Exception);
        Assert.True(ran2);
    }

    [Fact]
    public void Test_Run_AddDelayed()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelProducerConsumerRunner(2, sc.BuildServiceProvider());

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


        _ = runner.RunAsync(default);

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

    [Fact]
    public async Task Test_Run_AddDelayed_Await()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelProducerConsumerRunner(2, sc.BuildServiceProvider());

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


        var runTask = runner.RunAsync(default);

        Task.Run(async () =>
        {
            runner.AddStep(s3.Object);
            await Task.Delay(1000);
            runner.Finish();
        });

        await runTask;

        Assert.True(ran1);
        Assert.True(ran2);
        Assert.True(ran3);
    }

    [Fact]
    public async Task Test_Run_AddDelayed_AwaitAndWait()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelProducerConsumerRunner(2, sc.BuildServiceProvider());

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


        var runTask = runner.RunAsync(default);

        Task.Run(() =>
        {
            runner.AddStep(s3.Object);
            Task.Delay(1000);
            runner.Finish();
        });

        await runTask;
        runner.Wait();

        Assert.True(ran1);
        Assert.True(ran2);
        Assert.True(ran3);
    }

    [Fact]
    public async Task Test_Run_AddDelayed_Cancelled()
    {
        var sc = new ServiceCollection();
        var runner = new ParallelProducerConsumerRunner(2, sc.BuildServiceProvider());

        var tcs = new TaskCompletionSource<int>();

        var s1 = new Mock<IStep>();
        var s2 = new Mock<IStep>();

        runner.AddStep(s1.Object);

        var cts = new CancellationTokenSource();

        var ran1 = false;
        s1.Setup(t => t.Run(cts.Token)).Callback(() =>
        {
            ran1 = true;
            tcs.SetResult(0);
        });

        var ran2 = false;
        s2.Setup(t => t.Run(cts.Token)).Callback(() =>
        {
            ran2 = true;
        });

        var runTask = runner.RunAsync(cts.Token);

        Task.Run(async () =>
        {
            await tcs.Task.ConfigureAwait(false);
            cts.Cancel();
            runner.AddStep(s2.Object);
            runner.Finish();
        }).Forget();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await runTask);

        Assert.True(ran1);
        Assert.False(ran2);

        Assert.Equal([s1.Object], runner.Steps);
    }
}