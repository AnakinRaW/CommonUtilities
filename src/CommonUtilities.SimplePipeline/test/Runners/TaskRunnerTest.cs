using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class TaskRunnerTest
{
    [Fact]
    public void TestRunEmpty()
    {
        var sc = new ServiceCollection();
        var runner = new StepRunner(sc.BuildServiceProvider());

        runner.Run(default);

        Assert.Empty(runner.Steps);
        Assert.Empty(runner);
    }

    [Fact]
    public void TestRunCancelled()
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

        runner.Queue(step.Object);
        runner.Run(cts.Token);

        Assert.True(hasError);
        Assert.False(ran);
        Assert.Single(runner.Steps);
        Assert.Single(runner);
    }

    [Fact]
    public void TestRunWithError()
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

        runner.Queue(step.Object);
        runner.Run(default);

        Assert.True(hasError);
        Assert.True(ran);
    }
}
