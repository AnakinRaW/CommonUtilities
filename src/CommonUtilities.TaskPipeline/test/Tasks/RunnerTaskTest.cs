using System;
using System.Threading;
using AnakinRaW.CommonUtilities.TaskPipeline.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.TaskPipeline.Test.Tasks;

public class RunnerTaskTest
{
    [Fact]
    public void Test()
    {
        var sc = new ServiceCollection();
        var task = new Mock<RunnerTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        task.Object.Dispose();
        Assert.True(task.Object.IsDisposed);
    }

    [Fact]
    public void TestRun()
    {
        var sc = new ServiceCollection();
        var task = new Mock<RunnerTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        task.Object.Run(default);

        task.Protected().Verify("RunCore", Times.Exactly(1), false, (CancellationToken)default);
    }

    [Fact]
    public void TestRunWithException()
    {
        var sc = new ServiceCollection();
        var task = new Mock<RunnerTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        var e = new Exception();
        task.Protected().Setup("RunCore", false, (CancellationToken)default)
            .Throws(e);

        Assert.Throws<Exception>(() => task.Object.Run(default));
        Assert.Same(e, task.Object.Error);

    }

    [Fact]
    public void TestRunWithCancellation()
    {
        var sc = new ServiceCollection();
        var task = new Mock<RunnerTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        task.Protected().Setup("RunCore", false, cts.Token)
            .Callback<CancellationToken>(d => d.ThrowIfCancellationRequested());

        Assert.Throws<OperationCanceledException>(() => task.Object.Run(cts.Token));
        Assert.Null(task.Object.Error);
    }
}