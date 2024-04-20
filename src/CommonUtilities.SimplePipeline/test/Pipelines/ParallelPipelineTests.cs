using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class ParallelPipelineTests
{
    [Fact]
    public async Task Test_Run()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var j = 0;

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            Task.Delay(1000);
            Interlocked.Increment(ref j);
        });

        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            Task.Delay(1000);
            Interlocked.Increment(ref j);
        });

        var pipelineMock = new Mock<ParallelPipeline>(sp, 2, true)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<Task<IList<IStep>>>("BuildSteps").Returns(Task.FromResult<IList<IStep>>(new List<IStep>
        {
            s1.Object,
            s2.Object
        }));

        var pipeline = pipelineMock.Object;

        await pipeline.RunAsync();
        Assert.Equal(2, j);
    }

    [Fact]
    public async Task Test_Run_WithError()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var j = 0;

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            Task.Delay(1000);
            Interlocked.Increment(ref j);
        });

        var s2 = new Mock<IStep>();
        s2.SetupGet(s => s.Error).Returns(new Exception());
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            Task.Delay(1000);
        }).Throws<Exception>();

        var pipelineMock = new Mock<ParallelPipeline>(sp, 2, true)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<Task<IList<IStep>>>("BuildSteps").Returns(Task.FromResult<IList<IStep>>(new List<IStep>
        {
            s1.Object,
            s2.Object
        }));

        var pipeline = pipelineMock.Object;

        await Assert.ThrowsAsync<StepFailureException>(async () => await pipeline.RunAsync());
        Assert.Equal(1, j);
    }
}