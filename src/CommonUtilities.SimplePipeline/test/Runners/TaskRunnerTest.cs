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
        var runned = false;
        step.Setup(t => t.Run(cts.Token)).Callback(() =>
        {
            runned = true;
        });

        runner.Queue(step.Object);
        runner.Run(cts.Token);

        Assert.True(hasError);
        Assert.False(runned);
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
        var runned = false;
        step.Setup(t => t.Run(default)).Callback(() =>
        {
            runned = true;
        }).Throws<Exception>();

        runner.Queue(step.Object);
        runner.Run(default);

        Assert.True(hasError);
        Assert.True(runned);
    }
}
