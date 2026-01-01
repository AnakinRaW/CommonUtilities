using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class ParallelProducerConsumerPipelineTest : PipelineTestBase
{
    protected override Pipeline CreatePipeline(IList<IStep> steps)
    {
        return CreatePipeline(steps, false);
    }

    protected override ITrackingPipeline CreateTrackingPipeline(Func<CancellationToken, Task> prepare, Func<CancellationToken, Task> run)
    {
        IEnumerable<IStep> steps = [new TestStep(run, ServiceProvider)];
        return new TestParallelProducerConsumerPipeline(ServiceProvider, steps.ToAsyncEnumerable(), prepare, 4, false);
    }

    protected Pipeline CreatePipeline(IList<IStep> steps, bool failFast)
    {
        return CreateConsumerPipeline(steps.ToAsyncEnumerable(), failFast, 4);
    }

    private ParallelProducerConsumerPipeline CreateConsumerPipeline(IList<IStep> steps, bool failFast = true, int workerCount = 4)
    {
        return CreateConsumerPipeline(steps.ToAsyncEnumerable(), failFast, workerCount);
    }

    private ParallelProducerConsumerPipeline CreateConsumerPipeline(IAsyncEnumerable<IStep> steps, bool failFast = true, int workerCount = 4)
    {
        return new TestParallelProducerConsumerPipeline(ServiceProvider, steps, null, workerCount, failFast);
    }

    #region Constructor Tests

    [Fact]
    public void Ctor_NullServiceProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestParallelProducerConsumerPipeline(
            null!, 
            AsyncEnumerable.Empty<IStep>(), null, 4, true));
    }

    #endregion

    #region FailFast

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailFast_CtorSetsProperty(bool failFast)
    {
        var pipeline = CreateConsumerPipeline([], failFast);
        Assert.Equal(failFast, pipeline.FailFast);
    }

    #endregion

    #region RunAsync

    [Fact]
    public async Task RunAsync_EmptyAsyncEnumerable_Succeeds()
    {
        var pipeline = CreateConsumerPipeline(AsyncEnumerable.Empty<IStep>());

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.False(pipeline.PipelineFailed);
    }

    [Fact]
    public async Task RunAsync_ConcurrentCallsDuringBackgroundPreparation_OnlyFirstSucceeds()
    {
        var productionStarted = new TaskCompletionSource<bool>();
        var continueProduction = new TaskCompletionSource<bool>();
        var stepRunCount = 0;

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        // Start first RunAsync
        var firstTask = pipeline.RunAsync(CancellationToken.None);

        // Wait for production/preparation to start
        await productionStarted.Task;

        // Try to call RunAsync again - should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.RunAsync(CancellationToken.None));

        continueProduction.SetResult(true);
        await firstTask;

        Assert.Equal(1, stepRunCount);
        Assert.False(pipeline.PipelineFailed);

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            productionStarted.SetResult(true);
            await continueProduction.Task;
            yield return new TestStep(_ =>
            {
                Interlocked.Increment(ref stepRunCount);
                return Task.CompletedTask;
            }, ServiceProvider);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_PreparationFails_ThrowsPreparationException(bool failFast)
    {
        var mre = new ManualResetEventSlim(false);

        var ran = false;
        var s1 = new TestStep(async ct =>
        {
            await Task.Yield();
            mre.Wait(ct);
            ct.ThrowIfCancellationRequested();
            ran = true;
        }, ServiceProvider);
        var s2 = new TestStep(null, ServiceProvider);

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync(), failFast);

        var task = Assert.ThrowsAsync<ApplicationException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        if (failFast)
            await task;

        mre.Set();
        await task;

        if (failFast)
            Assert.False(ran);
        else
            Assert.True(ran);

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            yield return s1;
            yield return s2;
            await Task.Yield();
            throw new ApplicationException("test");
        }
    }

    [Fact]
    public async Task RunAsync_DelayedStepProduction_ExecutesAllSteps()
    {
        var tcs = new TaskCompletionSource<bool>();

        var s1 = new TestStep(async _ =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            tcs.SetResult(true);
        }, ServiceProvider);

        var s2Run = false;
        var s2 = new TestStep(_ =>
        {
            s2Run = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(s2Run);

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            yield return s1;
            await tcs.Task;
            yield return s2;
        }
    }

    [Fact]
    public async Task RunAsync_StepsProducedWhileRunning_AllExecuted()
    {
        var executedSteps = new ConcurrentBag<int>();
        var stepCount = 10;
        var delay = 50;

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(stepCount, executedSteps.Count);

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            for (var i = 0; i < stepCount; i++)
            {
                var stepIndex = i;
                yield return new TestStep(_ =>
                {
                    executedSteps.Add(stepIndex);
                    return Task.CompletedTask;
                }, ServiceProvider);
                await Task.Delay(delay, TestContext.Current.CancellationToken);
            }
        }
    }

    [Fact]
    public async Task RunAsync_ConsumesStepsWhileProducing()
    {
        var executionStartedDuringProduction = false;
        var firstStepExecutionStarted = new TaskCompletionSource<bool>();
        var canContinueProduction = new TaskCompletionSource<bool>();

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync(), workerCount: 4);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(executionStartedDuringProduction, "Step execution should start before production completes");

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            // Produce first step
            yield return new TestStep(async _ =>
            {
                firstStepExecutionStarted.SetResult(true);
                await canContinueProduction.Task;
            }, ServiceProvider);

            // Wait for first step to start executing
            await firstStepExecutionStarted.Task;

            // At this point, execution has started while we're still producing
            executionStartedDuringProduction = true;

            // Produce more steps
            yield return new TestStep(_ => Task.CompletedTask, ServiceProvider);
            yield return new TestStep(_ => Task.CompletedTask, ServiceProvider);

            // Allow first step to complete
            canContinueProduction.SetResult(true);

        }
    }

    //[Fact]
    //public async Task RunAsync_CalledWhilePrepareAsyncInProgress_WaitsForSamePreparation()
    //{
    //    var preparationStarted = new TaskCompletionSource<bool>();
    //    var continuePreparation = new TaskCompletionSource<bool>();
    //    var stepExecuted = false;
    //    var prepareCallCount = 0;

    //    var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

    //    // Start PrepareAsync but don't await completion
    //    var prepareTask = pipeline.PrepareAsync(CancellationToken.None);

    //    // Wait for preparation to actually start inside PrepareCoreAsync
    //    await preparationStarted.Task;

    //    // Now call RunAsync while preparation is in progress
    //    // This should NOT start a new preparation, but wait for the existing one
    //    var runTask = pipeline.RunAsync(CancellationToken.None);

    //    // Allow preparation to complete
    //    continuePreparation.SetResult(true);

    //    // Both tasks should complete
    //    await prepareTask;
    //    await runTask;

    //    // Preparation should only have happened once
    //    Assert.Equal(1, prepareCallCount);
    //    Assert.True(stepExecuted);
    //    Assert.False(pipeline.PipelineFailed);

    //    async IAsyncEnumerable<IStep> ProduceStepsAsync()
    //    {
    //        Interlocked.Increment(ref prepareCallCount);
    //        preparationStarted.SetResult(true);
    //        await continuePreparation.Task;
    //        yield return new TestStep(_ =>
    //        {
    //            stepExecuted = true;
    //            return Task.CompletedTask;
    //        }, ServiceProvider);
    //    }
    //}

    #endregion

    #region Preparation Failure Tests

    [Fact]
    public async Task PrepareAsync_ConcurrentCallsDuringStepProduction_OnlyFirstSucceeds()
    {
        var productionStarted = new TaskCompletionSource<bool>();
        var continueProduction = new TaskCompletionSource<bool>();
        var prepareCount = 0;

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        // Start first PrepareAsync
        var firstTask = pipeline.PrepareAsync(CancellationToken.None);

        // Wait for production to start
        await productionStarted.Task;

        // Try to call PrepareAsync again - should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.PrepareAsync(CancellationToken.None));

        continueProduction.SetResult(true);
        await firstTask;

        Assert.Equal(1, prepareCount);

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            Interlocked.Increment(ref prepareCount);
            productionStarted.SetResult(true);
            await continueProduction.Task;
            yield return new TestStep(_ => Task.CompletedTask, ServiceProvider);
        }
    }


    [Fact]
    public async Task PrepareAsync_CalledAfterRunAsyncStartedPreparation_ThrowsInvalidOperationException()
    {
        var preparationStarted = new TaskCompletionSource<bool>();
        var continuePreparation = new TaskCompletionSource<bool>();

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        // Start RunAsync which will trigger background preparation
        var runTask = pipeline.RunAsync(CancellationToken.None);

        // Wait for preparation to start
        await preparationStarted.Task;

        // Try to call PrepareAsync - should throw because preparation was already started by RunAsync
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.PrepareAsync(CancellationToken.None));

        // Let the pipeline complete
        continuePreparation.SetResult(true);
        await runTask;

        Assert.False(pipeline.PipelineFailed);

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            preparationStarted.SetResult(true);
            await continuePreparation.Task;
            yield return new TestStep(_ => Task.CompletedTask, ServiceProvider);
        }
    }

    [Fact]
    public async Task PrepareAsync_PreparationFails_ThrowsException()
    {
        var s1 = new TestStep(null, ServiceProvider);
        var s2 = new TestStep(null, ServiceProvider);

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        await Assert.ThrowsAsync<ApplicationException>(async () => await pipeline.PrepareAsync(TestContext.Current.CancellationToken));

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            yield return s1;
            yield return s2;
            await Task.Yield();
            throw new ApplicationException("test");
        }
    }

    [Fact]
    public async Task RunAsync_PreparationFailsImmediately_ThrowsException()
    {
        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        await Assert.ThrowsAsync<ApplicationException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            await Task.Yield();
            throw new ApplicationException("Immediate failure");
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task RunAsync_PreparationCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<bool>();

        var s1 = new TestStep(async _ =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            tcs.SetResult(true);
        }, ServiceProvider);

        var s2Run = false;
        var s2 = new TestStep(_ =>
        {
            s2Run = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.RunAsync(cts.Token));

        Assert.False(s2Run);
        return;

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            yield return s1;
            await tcs.Task;
            cts.Cancel();
            await Task.Delay(100, TestContext.Current.CancellationToken);
            yield return s2;
        }
    }

    [Fact]
    public async Task RunAsync_CancelledDuringProduction_StopsProducing()
    {
        var cts = new CancellationTokenSource();
        var productionGate = new TaskCompletionSource<bool>();
        var cancelGate = new TaskCompletionSource<bool>();
        var producedSteps = new List<int>();

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        var runTask = Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.RunAsync(cts.Token));

        // Wait until step 2 is about to be produced
        await productionGate.Task;

        // Cancel and signal to continue
        cts.Cancel();
        cancelGate.SetResult(true);

        await runTask;

        // Only steps 0 and 1 should have been produced
        Assert.Equal(2, producedSteps.Count);

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            for (var i = 0; i < 10; i++)
            {
                if (i == 2)
                {
                    productionGate.SetResult(true);
                    await cancelGate.Task;
                }

                // This should throw after cancellation
                cts.Token.ThrowIfCancellationRequested();

                producedSteps.Add(i);
                yield return new TestStep(_ => Task.CompletedTask, ServiceProvider);
            }
        }
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task RunAsync_ExecutesStepsInParallel()
    {
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync(), workerCount: 4);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(maxConcurrent > 1, $"Expected parallel execution, but max concurrent was {maxConcurrent}");

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            for (var i = 0; i < 8; i++)
            {
                yield return new TestStep(async _ =>
                {
                    lock (lockObj)
                    {
                        concurrentCount++;
                        maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                    }

                    await Task.Delay(100, TestContext.Current.CancellationToken);

                    lock (lockObj)
                    {
                        concurrentCount--;
                    }
                }, ServiceProvider);
            }
            await Task.Yield();
        }
    }

    #endregion

    private class TestParallelProducerConsumerPipeline : ParallelProducerConsumerPipeline, ITrackingPipeline
    {
        private readonly IAsyncEnumerable<IStep> _steps;
        private readonly Func<CancellationToken, Task>? _prepareAction;

        public TestParallelProducerConsumerPipeline(
            IServiceProvider serviceProvider, 
            IAsyncEnumerable<IStep> steps,
            Func<CancellationToken, Task>? onPrepare,
            int workerCount,
            bool failFast) : base(workerCount, serviceProvider)
        {
            _steps = steps;
            FailFast = failFast;
            _prepareAction = onPrepare;
        }

        protected override async IAsyncEnumerable<IStep> BuildStepsAsync([EnumeratorCancellation] CancellationToken token)
        {
            if (_prepareAction is not null)
                await _prepareAction(token);
            await foreach (var step in _steps.WithCancellation(token))
            {
                yield return step;
            }
        }
    }
}