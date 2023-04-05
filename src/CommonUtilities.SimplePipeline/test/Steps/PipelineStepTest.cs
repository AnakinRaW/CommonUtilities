using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class PipelineStepTest
{
    [Fact]
    public void Test()
    {
        var sc = new ServiceCollection();
        var step = new Mock<PipelineStep>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        step.Object.Dispose();
        Assert.True(step.Object.IsDisposed);
    }

    [Fact]
    public void TestRun()
    {
        var sc = new ServiceCollection();
        var step = new Mock<PipelineStep>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        step.Object.Run(default);

        step.Protected().Verify("RunCore", Times.Exactly(1), false, (CancellationToken)default);
    }

    [Fact]
    public void TestRunWithException()
    {
        var sc = new ServiceCollection();
        var step = new Mock<PipelineStep>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        var e = new Exception();
        step.Protected().Setup("RunCore", false, (CancellationToken)default)
            .Throws(e);

        Assert.Throws<Exception>(() => step.Object.Run(default));
        Assert.Same(e, step.Object.Error);

    }

    [Fact]
    public void TestRunWithCancellation()
    {
        var sc = new ServiceCollection();
        var step = new Mock<PipelineStep>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        step.Protected().Setup("RunCore", false, cts.Token)
            .Callback<CancellationToken>(d => d.ThrowIfCancellationRequested());

        Assert.Throws<OperationCanceledException>(() => step.Object.Run(cts.Token));
        Assert.Null(step.Object.Error);
    }
}