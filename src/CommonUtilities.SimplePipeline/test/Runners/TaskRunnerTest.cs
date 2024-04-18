using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class TaskRunnerTest
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
}
