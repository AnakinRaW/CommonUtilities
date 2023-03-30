using System;
using System.Threading;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class JobBaseTest
{
    [Fact]
    public void TestJobPlan()
    {
        var job = new Mock<PipelineBase>
        {
            CallBase = true
        };

        job.Object.Prepare();
        job.Object.Prepare();

        job.Protected().Verify("Initialize", Times.Exactly(1));
    }

    [Fact]
    public void TestJobRun()
    {
        var job = new Mock<PipelineBase>
        {
            CallBase = true
        };

        job.Protected().Setup<bool>("PlanCore").Returns(true);
        
        job.Object.Run();
        job.Object.Run();

        job.Protected().Verify("Initialize", Times.Exactly(1));
        job.Protected().Verify<bool>("PlanCore", Times.Exactly(1));
        job.Protected().Verify("RunCore", Times.Exactly(2), false, (CancellationToken) default);
    }

    [Fact]
    public void TestJobPlanRun()
    {
        var job = new Mock<PipelineBase>
        {
            CallBase = true
        };

        job.Protected().Setup<bool>("PlanCore").Returns(true);

        job.Object.Prepare();
        job.Object.Run();
        job.Object.Run();

        job.Protected().Verify("Initialize", Times.Exactly(1));
        job.Protected().Verify<bool>("PlanCore", Times.Exactly(1));
        job.Protected().Verify("RunCore", Times.Exactly(2), false, (CancellationToken)default);
    }

    [Fact]
    public void TestJobRunCancelled()
    {
        var job = new Mock<PipelineBase>
        {
            CallBase = true
        };

        job.Protected().Setup<bool>("PlanCore").Returns(true);

        var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => job.Object.Run(cts.Token));
    }
}