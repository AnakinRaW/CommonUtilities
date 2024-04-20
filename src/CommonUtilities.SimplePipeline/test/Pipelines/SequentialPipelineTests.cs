using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class SequentialPipelineTests
{
    [Fact]
    public async Task Test_Run_SequentialPipeline_RunsInSequence()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var sb = new StringBuilder();

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            sb.Append('a');
        });

        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            sb.Append('b');
        });

        var pipelineMock = new Mock<SequentialPipeline>(sp, true)
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
        Assert.Equal("ab", sb.ToString());
    }

    [Theory]
    [InlineData(true, "")]
    [InlineData(false, "b")]
    public async Task Test_Run_WithError(bool failFast, string result)
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var sb = new StringBuilder();

        var s1 = new Mock<IStep>();
        s1.SetupGet(s => s.Error).Returns(new Exception());
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Throws<Exception>();

        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            sb.Append('b');
        });

        var pipelineMock = new Mock<SequentialPipeline>(sp, failFast)
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
        Assert.Equal(result, sb.ToString());
    }
}