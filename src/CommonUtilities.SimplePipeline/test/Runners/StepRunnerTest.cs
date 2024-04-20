using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class StepRunnerTest
{
    [Fact]
    public async Task Test_Run_Empty()
    {
        var sc = new ServiceCollection();
        var runner = new StepRunner(sc.BuildServiceProvider());

        await runner.RunAsync(default);

        Assert.Empty(runner.Steps);
    }

    [Fact]
    public async Task Test_Run_Cancelled()
    {
        var sc = new ServiceCollection();
        var runner = new StepRunner(sc.BuildServiceProvider());

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var hasError = false;
        runner.Error += (_, __) =>
        {
            hasError = true;
        };

        var step = new Mock<IStep>();
        var ran = false;
        step.Setup(t => t.Run(cts.Token)).Callback(() =>
        {
            ran = true;
        });

        runner.AddStep(step.Object);
        await runner.RunAsync(cts.Token);

        Assert.True(hasError);
        Assert.False(ran);
        Assert.Single(runner.Steps);
    }

    [Fact]
    public async Task Test_Run_WithError()
    {
        var sc = new ServiceCollection();
        var runner = new StepRunner(sc.BuildServiceProvider());

        var hasError = false;
        runner.Error += (_, __) =>
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
    }

    [Fact]
    public async Task Test_Run()
    {
        var sc = new ServiceCollection();
        var runner = new StepRunner(sc.BuildServiceProvider());

        var hasError = false;
        runner.Error += (_, __) =>
        {
            hasError = true;
        };

        var ran1 = false;
        var ran2 = false;
        var step1 = new TestStep(_ => ran1 = true);
        var step2 = new TestStep(_ => ran2 = true);

        runner.AddStep(step1);
        runner.AddStep(step2);
        await runner.RunAsync(default);

        Assert.False(hasError);
        Assert.True(ran1);
        Assert.True(ran2);

        runner.Dispose();

        Assert.True(step1.IsDisposed);
        Assert.True(step2.IsDisposed);

        Assert.Equal([step1, step2], runner.Steps);
    }
}
