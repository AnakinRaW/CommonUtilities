using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class SimplePipelineTests : CommonTestBase
{
    [Fact]
    public async Task Test_Run_SimplePipelineRunsNormally()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var pipelineMock = new Mock<SimplePipeline<SequentialStepRunner>>(sp, false)
        {
            CallBase = true
        };
        pipelineMock.Protected().Setup<SequentialStepRunner>("CreateRunner").Returns(new SequentialStepRunner(sp));
        pipelineMock.Protected().Setup<Task<IList<IStep>>>("BuildSteps").Returns(Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new TestStep(1, "123", ServiceProvider)
        }));

        var pipeline = pipelineMock.Object;

        var cancellationTokenSource = new CancellationTokenSource();
        await pipeline.RunAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task Test_Run_SimplePipelineFails_ThrowsStepFailureException()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var s = new Mock<IStep>();
        s.Setup(i => i.Run(It.IsAny<CancellationToken>())).Throws<Exception>();
        s.Setup(i => i.Error).Returns(new Exception());

        var pipelineMock = new Mock<SimplePipeline<SequentialStepRunner>>(sp, false)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<SequentialStepRunner>("CreateRunner").Returns(new SequentialStepRunner(sp));
        pipelineMock.Protected().Setup<Task<IList<IStep>>>("BuildSteps").Returns(Task.FromResult<IList<IStep>>(new List<IStep>
        {
            s.Object,
        }));

        var pipeline = pipelineMock.Object;

        var cancellationTokenSource = new CancellationTokenSource();
        await Assert.ThrowsAsync<StepFailureException>(async () => await pipeline.RunAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task Test_Run_SimplePipelineFailsSlow_ThrowsStepFailureException()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Throws<Exception>();
        s1.Setup(i => i.Error).Returns(new Exception());

        var flag = false;
        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() => flag = true);

        var pipelineMock = new Mock<SimplePipeline<SequentialStepRunner>>(sp, false)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<SequentialStepRunner>("CreateRunner").Returns(new SequentialStepRunner(sp));
        pipelineMock.Protected().Setup<Task<IList<IStep>>>("BuildSteps").Returns(Task.FromResult<IList<IStep>>(new List<IStep>
        {
            s1.Object,
            s2.Object
        }));

        var pipeline = pipelineMock.Object;

        await Assert.ThrowsAsync<StepFailureException>(async () => await pipeline.RunAsync());
        Assert.True(flag);
    }

    [Fact]
    public async Task Test_Run_SimplePipelineFailsFast_ThrowsStepFailureException()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Throws<Exception>();
        s1.Setup(i => i.Error).Returns(new Exception());

        var flag = false;
        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() => flag = true);

        var pipelineMock = new Mock<SimplePipeline<SequentialStepRunner>>(sp, true)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<SequentialStepRunner>("CreateRunner").Returns(new SequentialStepRunner(sp));
        pipelineMock.Protected().Setup<Task<IList<IStep>>>("BuildSteps").Returns(Task.FromResult<IList<IStep>>(new List<IStep>
        {
            s1.Object,
            s2.Object
        }));

        var pipeline = pipelineMock.Object;

        await Assert.ThrowsAsync<StepFailureException>(async () => await pipeline.RunAsync());
        Assert.False(flag);
    }
}