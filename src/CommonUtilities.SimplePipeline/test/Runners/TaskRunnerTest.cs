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
        var runner = new TaskRunner(sc.BuildServiceProvider());

        runner.Run(default);

        Assert.Empty(runner.Tasks);
        Assert.Empty(runner);
    }

    [Fact]
    public void TestRunCancelled()
    {
        var sc = new ServiceCollection();
        var runner = new TaskRunner(sc.BuildServiceProvider());

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var hasError = false;
        runner.Error += (_, __) =>
        {
            hasError = true;
        };

        var task = new Mock<ITask>();
        var runned = false;
        task.Setup(t => t.Run(cts.Token)).Callback(() =>
        {
            runned = true;
        });

        runner.Queue(task.Object);
        runner.Run(cts.Token);

        Assert.True(hasError);
        Assert.False(runned);
        Assert.Single(runner.Tasks);
        Assert.Single(runner);
    }

    [Fact]
    public void TestRunWithError()
    {
        var sc = new ServiceCollection();
        var runner = new TaskRunner(sc.BuildServiceProvider());

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

        Assert.True(hasError);
        Assert.True(runned);
    }
}
