using System;
using System.Threading;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class PipelineTest
{
    [Fact]
    public void Test_Prepare()
    {
        var pipeline = new Mock<Pipeline>
        {
            CallBase = true
        };

        pipeline.Object.Prepare();
        pipeline.Object.Prepare();

        pipeline.Protected().Verify("PrepareCore", Times.Exactly(1));
    }

    [Fact]
    public void Test_Run()
    {
        var pipeline = new Mock<Pipeline>
        {
            CallBase = true
        };

        pipeline.Protected().Setup<bool>("PrepareCore").Returns(true);
        
        pipeline.Object.Run();
        pipeline.Object.Run();

        pipeline.Protected().Verify<bool>("PrepareCore", Times.Exactly(1));
        pipeline.Protected().Verify("RunCore", Times.Exactly(2), false, (CancellationToken) default);
    }

    [Fact]
    public void Test_Prepare_Run()
    {
        var pipeline = new Mock<Pipeline>
        {
            CallBase = true
        };

        pipeline.Protected().Setup<bool>("PrepareCore").Returns(true);

        pipeline.Object.Prepare();
        pipeline.Object.Run();
        pipeline.Object.Run();

        pipeline.Protected().Verify<bool>("PrepareCore", Times.Exactly(1));
        pipeline.Protected().Verify("RunCore", Times.Exactly(2), false, (CancellationToken)default);
    }

    [Fact]
    public void Test_Run_Cancelled_ThrowsOperationCanceledException()
    {
        var pipeline = new Mock<Pipeline>
        {
            CallBase = true
        };

        pipeline.Protected().Setup<bool>("PrepareCore").Returns(true);

        var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => pipeline.Object.Run(cts.Token));
    }

    [Fact]
    public void Test_Prepare_Disposed_ThrowsObjectDisposedException()
    {
        var pipeline = new Mock<Pipeline>
        {
            CallBase = true
        };

        pipeline.Object.Dispose();
        pipeline.Object.Dispose();

        Assert.Throws<ObjectDisposedException>(() => pipeline.Object.Prepare());
    }

    [Fact]
    public void Test_Run_Disposed_ThrowsObjectDisposedException()
    {
        var pipeline = new Mock<Pipeline>
        {
            CallBase = true
        };

        pipeline.Object.Prepare();
        pipeline.Object.Dispose();

        Assert.Throws<ObjectDisposedException>(() => pipeline.Object.Run());
    }
}