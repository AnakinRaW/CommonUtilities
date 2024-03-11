using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class SequentialPipelineTests
{
    [Fact]
    public void Test_Run_SequentialPipeline_RunsInSequence()
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

        pipelineMock.Protected().Setup<IList<IStep>>("BuildStepsOrdered").Returns(new List<IStep>
        {
            s1.Object,
            s2.Object
        });

        var pipeline = pipelineMock.Object;

        pipeline.Run();
        Assert.Equal("ab", sb.ToString());
    }
}