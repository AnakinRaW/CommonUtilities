using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class ParallelProducerConsumerPipelineTest
{
    [Fact]
    public async Task Test_Prepare()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(sp.Object, 4, true)
        {
            CallBase = true
        };

        await pipeline.Object.PrepareAsync();
        await pipeline.Object.PrepareAsync();

        pipeline.Protected().Verify<Task>("BuildSteps", Times.Once(), ItExpr.IsAny<IStepQueue>());
    }

    [Fact]
    public async Task Test_Run_RunsNormally()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(sp.Object, 4, true)
        {
            CallBase = true
        };

        var stepRun = false;
        var s = new Mock<IStep>();
        s.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() => stepRun = true);

        pipeline.Protected().Setup<Task>("BuildSteps", ItExpr.IsAny<IStepQueue>()).Callback((IStepQueue q) =>
        {
            q.AddStep(s.Object);
        });

        await pipeline.Object.RunAsync();

        Assert.True(stepRun);
        pipeline.Protected().Verify<Task>("BuildSteps", Times.Once(), ItExpr.IsAny<IStepQueue>());
    }

    [Fact]
    public async Task Test_Run_DelayedAdd()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(sp.Object, 4, true)
        {
            CallBase = true
        };

        var mre = new ManualResetEventSlim();

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            {
                Task.Delay(3000).Wait();
                mre.Set();
            }
        });


        var s2Run = false;
        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            {
                s2Run = true;
            }
        });

        pipeline.Protected().Setup<Task>("BuildSteps", ItExpr.IsAny<IStepQueue>()).Callback((IStepQueue q) =>
        {
            q.AddStep(s1.Object);
            mre.Wait();
            q.AddStep(s2.Object);

        });

        await pipeline.Object.RunAsync();

        Assert.True(s2Run);
        pipeline.Protected().Verify<Task>("BuildSteps", Times.Once(), ItExpr.IsAny<IStepQueue>());
    }

    [Fact]
    public async Task Test_Run_DelayedAdd_PrepareFails()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(sp.Object, 4, true)
        {
            CallBase = true
        };

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>()));

        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>()));

        pipeline.Protected().Setup<Task>("BuildSteps", ItExpr.IsAny<IStepQueue>())
            .Callback((IStepQueue q) =>
            {
                q.AddStep(s1.Object);
                q.AddStep(s2.Object);
            })
            .Throws<Exception>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipeline.Object.RunAsync());

        pipeline.Protected().Verify<Task>("BuildSteps", Times.Once(), ItExpr.IsAny<IStepQueue>());
    }


    [Fact]
    public async Task Test_Run_PrepareCancelled()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(sp.Object, 4, true)
        {
            CallBase = true
        };

        var cts = new CancellationTokenSource();
        var mre = new ManualResetEventSlim();

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            {
                Task.Delay(3000).Wait();
                mre.Set();
            }
        });


        var s2Run = false;
        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            {
                s2Run = true;
            }
        });

        pipeline.Protected().Setup<Task>("BuildSteps", ItExpr.IsAny<IStepQueue>()).Callback((IStepQueue q) =>
        {
            q.AddStep(s1.Object);
            mre.Wait();
            cts.Cancel();
            q.AddStep(s2.Object);

        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pipeline.Object.RunAsync(cts.Token));

        Assert.False(s2Run);
        pipeline.Protected().Verify<Task>("BuildSteps", Times.Once(), ItExpr.IsAny<IStepQueue>());
    }
}

public class SimplePipelineTests
{
    [Fact]
    public async Task Test_Run_SimplePipelineRunsNormally()
    {
        var sc = new ServiceCollection();

        var sp = sc.BuildServiceProvider();

        var pipelineMock = new Mock<SimplePipeline<StepRunner>>(sp, false)
        {
            CallBase = true
        };
        pipelineMock.Protected().Setup<StepRunner>("CreateRunner").Returns(new StepRunner(sp));
        pipelineMock.Protected().Setup<Task<IList<IStep>>>("BuildSteps").Returns(Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new TestStep(1, "123")
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

        var pipelineMock = new Mock<SimplePipeline<StepRunner>>(sp, false)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<StepRunner>("CreateRunner").Returns(new StepRunner(sp));
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

        var pipelineMock = new Mock<SimplePipeline<StepRunner>>(sp, false)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<StepRunner>("CreateRunner").Returns(new StepRunner(sp));
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

        var pipelineMock = new Mock<SimplePipeline<StepRunner>>(sp, true)
        {
            CallBase = true
        };

        pipelineMock.Protected().Setup<StepRunner>("CreateRunner").Returns(new StepRunner(sp));
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