using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class RunPipelineStepTest
{
    [Fact]
    public void Test_Run()
    {
        var pipeline = new Mock<IPipeline>();

        var run = false;
        pipeline.Setup(p => p.RunAsync(default)).Returns(Task.Run(async () =>
        {
            await Task.Delay(3000).ConfigureAwait(false);
            run = true;
        }));

        var step = new RunPipelineStep(pipeline.Object, new Mock<IServiceProvider>().Object);

        step.Run(default);
        Assert.True(run);
    }

    [Fact]
    public void Test_Run_PipelineFails()
    {
        var pipeline = new Mock<IPipeline>();

        pipeline.Setup(p => p.RunAsync(default))
            .Returns(() => Task.Run(() => throw new ApplicationException("test")));

        var step = new RunPipelineStep(pipeline.Object, new Mock<IServiceProvider>().Object);

        Assert.Throws<ApplicationException>(() => step.Run(default));
    }

    [Fact]
    public void Test_Run_Cancel()
    {
        var pipeline = new Mock<IPipeline>();

        var run = false;

        var cts = new CancellationTokenSource();
        pipeline.Setup(p => p.RunAsync(cts.Token))
            .Returns(() => Task.Run(() =>
            {
                cts.Token.ThrowIfCancellationRequested();
            }));

        var step = new RunPipelineStep(pipeline.Object, new Mock<IServiceProvider>().Object);


        cts.Cancel(); 
        Assert.Throws<OperationCanceledException>(() => step.Run(cts.Token));
    }
}