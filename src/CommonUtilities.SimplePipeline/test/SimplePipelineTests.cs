using System;
using System.Collections.Generic;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class SimplePipelineTests
{
    [Fact]
    public void Test_Run_SimplePipelineRunsNormally()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var pipelineMock = new Mock<SimplePipeline<StepRunner>>(sp, false)
        {
            CallBase = true
        };
        pipelineMock.Protected().Setup<StepRunner>("CreateRunner").Returns(new StepRunner(sp));
        pipelineMock.Protected().Setup<IList<IStep>>("BuildStepsOrdered").Returns(new List<IStep>
        {
            new TestStep(1, "123")
        });

        var pipeline = pipelineMock.Object;

        var cancellationTokenSource = new CancellationTokenSource();
        pipeline.Run(cancellationTokenSource.Token);
    }

    [Fact]
    public void Test_Run_SimplePipelineFails_ThrowsStepFailureException()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var s = new Mock<IStep>();
        s.Setup(i => i.Run(It.IsAny<CancellationToken>())).Throws<Exception>();
        s.Setup(i => i.Error).Returns(new Exception());

        var pipelineMock = new Mock<SimplePipeline<StepRunner>>(sp, false)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<StepRunner>("CreateRunner").Returns(new StepRunner(sp));
        pipelineMock.Protected().Setup<IList<IStep>>("BuildStepsOrdered").Returns(new List<IStep>
        {
            s.Object
        });

        var pipeline = pipelineMock.Object;

        var cancellationTokenSource = new CancellationTokenSource();
        Assert.Throws<StepFailureException>(() => pipeline.Run(cancellationTokenSource.Token));
    }

    [Fact]
    public void Test_Run_SimplePipelineFailsSlow_ThrowsStepFailureException()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Throws<Exception>();
        s1.Setup(i => i.Error).Returns(new Exception());

        var flag = false;
        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() => flag = true);

        var pipelineMock = new Mock<SimplePipeline<StepRunner>>(sp, false)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<StepRunner>("CreateRunner").Returns(new StepRunner(sp));
        pipelineMock.Protected().Setup<IList<IStep>>("BuildStepsOrdered").Returns(new List<IStep>
        {
            s1.Object,
            s2.Object
        });

        var pipeline = pipelineMock.Object;

        Assert.Throws<StepFailureException>(() => pipeline.Run());
        Assert.True(flag);
    }

    [Fact]
    public void Test_Run_SimplePipelineFailsFast_ThrowsStepFailureException()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Throws<Exception>();
        s1.Setup(i => i.Error).Returns(new Exception());

        var flag = false;
        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() => flag = true);

        var pipelineMock = new Mock<SimplePipeline<StepRunner>>(sp, true)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<StepRunner>("CreateRunner").Returns(new StepRunner(sp));
        pipelineMock.Protected().Setup<IList<IStep>>("BuildStepsOrdered").Returns(new List<IStep>
        {
            s1.Object,
            s2.Object
        });

        var pipeline = pipelineMock.Object;

        Assert.Throws<StepFailureException>(() => pipeline.Run());
        Assert.False(flag);
    }
}