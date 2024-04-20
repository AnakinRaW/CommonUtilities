using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class ParallelProducerConsumerPipelineTest
{
    [Fact]
    public async Task Test_Prepare()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(4, true, sp.Object)
        {
            CallBase = true
        };

        var steps = new List<IStep>();
        pipeline.Protected().Setup<IAsyncEnumerable<IStep>>("BuildSteps").Returns(steps.ToAsyncEnumerable());

        await pipeline.Object.PrepareAsync();
        await pipeline.Object.PrepareAsync();

        pipeline.Protected().Verify<IAsyncEnumerable<IStep>>("BuildSteps", Times.Once());
        pipeline.Protected().Verify<Task<bool>>("PrepareCoreAsync", Times.Once());
    }

    [Fact]
    public async Task Test_Run_RunsNormally()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(4, true, sp.Object)
        {
            CallBase = true
        };

        var stepRun = false;
        var s = new Mock<IStep>();
        s.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() => stepRun = true);

        var steps = new List<IStep>{s.Object};
        pipeline.Protected().Setup<IAsyncEnumerable<IStep>>("BuildSteps").Returns(steps.ToAsyncEnumerable());

        await pipeline.Object.RunAsync();

        Assert.True(stepRun);

        pipeline.Protected().Verify<IAsyncEnumerable<IStep>>("BuildSteps", Times.Once());
        pipeline.Protected().Verify<Task<bool>>("PrepareCoreAsync", Times.Once());
    }

    [Fact]
    public async Task Test_Run_DelayedAdd()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(4, true, sp.Object)
        {
            CallBase = true
        };

        var tsc = new TaskCompletionSource<int>();

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            {
                Task.Delay(3000).Wait();
                tsc.SetResult(0);
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


        pipeline.Protected().Setup<IAsyncEnumerable<IStep>>("BuildSteps").Returns(ValueFunction);

        await pipeline.Object.RunAsync();

        Assert.True(s2Run);

        pipeline.Protected().Verify<IAsyncEnumerable<IStep>>("BuildSteps", Times.Once());
        pipeline.Protected().Verify<Task<bool>>("PrepareCoreAsync", Times.Once());
        return;

        async IAsyncEnumerable<IStep> ValueFunction()
        {
            yield return s1.Object;
            await tsc.Task;
            yield return s2.Object;
        }
    }

    [Fact]
    public async Task Test_Run_DelayedAdd_PrepareFails()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(4, true, sp.Object)
        {
            CallBase = true
        };

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>()));

        var s2 = new Mock<IStep>();
        s2.Setup(i => i.Run(It.IsAny<CancellationToken>()));

        pipeline.Protected().Setup<IAsyncEnumerable<IStep>>("BuildSteps").Returns(ValueFunction);

        await Assert.ThrowsAsync<ApplicationException>(async () => await pipeline.Object.RunAsync());

        pipeline.Protected().Verify<IAsyncEnumerable<IStep>>("BuildSteps", Times.Once());
        pipeline.Protected().Verify<Task<bool>>("PrepareCoreAsync", Times.Once());
        return;

        async IAsyncEnumerable<IStep> ValueFunction()
        {
            yield return s1.Object;
            yield return s2.Object;
            throw new ApplicationException("test");
        }
    }


    [Fact]
    public async Task Test_Run_PrepareCancelled()
    {
        var sp = new Mock<IServiceProvider>();
        var pipeline = new Mock<ParallelProducerConsumerPipeline>(4, true, sp.Object)
        {
            CallBase = true
        };

        var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<int>();

        var s1 = new Mock<IStep>();
        s1.Setup(i => i.Run(It.IsAny<CancellationToken>())).Callback(() =>
        {
            {
                Task.Delay(3000).Wait();
                tcs.SetResult(0);
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

        pipeline.Protected().Setup<IAsyncEnumerable<IStep>>("BuildSteps").Returns(ValueFunction);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pipeline.Object.RunAsync(cts.Token));

        Assert.False(s2Run);

        pipeline.Protected().Verify<IAsyncEnumerable<IStep>>("BuildSteps", Times.Once());
        pipeline.Protected().Verify<Task<bool>>("PrepareCoreAsync", Times.Once());
        return;

        async IAsyncEnumerable<IStep> ValueFunction()
        {
            yield return s1.Object;
            await tcs.Task;
            cts.Cancel();
            yield return s2.Object;
        }
    }
}