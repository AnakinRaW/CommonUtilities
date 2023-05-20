using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class ParallelPipelineTests
{
    [Fact]
    public void ParallelPipeline_Waits()
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

        pipelineMock.Protected().Setup<IList<IStep>>("BuildStepsOrdered").Returns(new List<IStep>
        {
            s1.Object,
            s2.Object
        });

        var pipeline = pipelineMock.Object;

        pipeline.Run();
        Assert.Equal(2, j);
    }
}