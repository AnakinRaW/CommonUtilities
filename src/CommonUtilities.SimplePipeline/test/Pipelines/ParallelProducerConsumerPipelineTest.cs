using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Extensions;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class ParallelProducerConsumerPipelineTest : StepRunnerPipelineBaseTestBase<ProducerConsumerStepRunner>
{
    protected override StepRunnerPipelineBase<ProducerConsumerStepRunner> CreateStepRunnerPipelineBase(IList<IStep> steps, bool failFast, RunnerBehavior runnerBehavior)
    {
        return CreateConsumerPipeline(steps.ToAsyncEnumerable(), failFast, runnerBehavior);
    }

    protected override ITrackingPipeline CreateTrackingPipeline(Func<CancellationToken, Task> prepare, Func<CancellationToken, Task> run)
    {
        IEnumerable<IStep> steps = [new TestStep(run, ServiceProvider)];
        return new TestParallelProducerConsumerPipeline(ServiceProvider, steps.ToAsyncEnumerable(), prepare, GetWorkerCount(GetRandomRunBehavior()), false);
    }

    private ParallelProducerConsumerPipeline CreateConsumerPipeline(IAsyncEnumerable<IStep> steps, bool failFast, RunnerBehavior runnerBehavior)
    {
        return new TestParallelProducerConsumerPipeline(ServiceProvider, steps, null, GetWorkerCount(runnerBehavior), failFast);
    }

    private ParallelProducerConsumerPipeline CreateConsumerPipeline(IAsyncEnumerable<IStep> steps)
    {
        return CreateConsumerPipeline(steps, Random.Bool(), GetRandomRunBehavior());
    }

    #region Constructor Tests

    [Fact]
    public void Ctor_NullServiceProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestParallelProducerConsumerPipeline(
            null!, AsyncEnumerable.Empty<IStep>(), null, 4, true));
    }

    #endregion

    #region RunAsync

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

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.RunAsync(CancellationToken.None));

        continueProduction.SetResult(true);
        await firstTask;

        Assert.Equal(1, stepRunCount);
        Assert.False(pipeline.Failed);

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
        var barrier = new ManualResetEventSlim(false);
        var canThrow = new TaskCompletionSource<int>(false);

        var ran = false;
        var s1 = new TestStep(ct =>
        {
            canThrow.SetResult(1);
            barrier.Wait(ct);
            ct.ThrowIfCancellationRequested();
            ran = true;
            return Task.CompletedTask;
        }, ServiceProvider);
        var s2 = new TestStep(null, ServiceProvider);

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync(), failFast, GetRandomRunBehavior());

        var task = Assert.ThrowsAsync<ApplicationException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        if (failFast)
            await task;

        barrier.Set();
        await task;

        if (failFast)
            Assert.False(ran);
        else
            Assert.True(ran);

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            yield return s1;
            yield return s2;
            await canThrow.Task;
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

    [Theory]
    [InlineData(RunnerBehavior.Concurrent)]
    [InlineData(RunnerBehavior.Sequential)]
    public async Task RunAsync_StepsProducedWhileRunning_AllExecuted(RunnerBehavior runnerBehavior)
    {
        var executedSteps = new ConcurrentQueue<int>();
        const int stepCount = 10;
        const int delay = 50;

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync(), Random.Bool(), runnerBehavior);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(stepCount, executedSteps.Count);
        if (runnerBehavior is RunnerBehavior.Sequential)
            Assert.Equal(executedSteps.ToList().OrderBy(x => x), executedSteps);
        return;


        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            for (var i = 0; i < stepCount; i++)
            {
                var stepIndex = i;
                yield return new TestStep(_ =>
                {
                    executedSteps.Enqueue(stepIndex);
                    return Task.CompletedTask;
                }, ServiceProvider);
                await Task.Delay(delay, TestContext.Current.CancellationToken);
            }
        }
    }

    [Theory]
    [InlineData(RunnerBehavior.Concurrent)]
    [InlineData(RunnerBehavior.Sequential)] // Checks, production is started on ThreadPool
    public async Task RunAsync_ConsumesStepsWhileProducing(RunnerBehavior runnerBehavior)
    {
        var executionStartedDuringProduction = false;
        var firstStepExecutionStarted = new TaskCompletionSource<bool>();
        var canContinueProduction = new TaskCompletionSource<bool>();

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync(), Random.Bool(), runnerBehavior);

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

    [Fact]
    public async Task RunAsync_CalledDuringStepProduction_WaitsForSamePreparation()
    {
        var preparationStarted = new TaskCompletionSource<bool>();
        var continuePreparation = new TaskCompletionSource<bool>();
        var stepExecuted = false;
        var prepareCallCount = 0;

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        var prepareTask = pipeline.PrepareAsync(CancellationToken.None);

        await preparationStarted.Task;

        var runTask = pipeline.RunAsync(CancellationToken.None);

        continuePreparation.SetResult(true);

        await prepareTask;
        await runTask;

        Assert.Equal(1, prepareCallCount);
        Assert.True(stepExecuted);
        Assert.False(pipeline.Failed);

        async IAsyncEnumerable<IStep> ProduceStepsAsync()
        {
            Interlocked.Increment(ref prepareCallCount);
            preparationStarted.SetResult(true);
            await continuePreparation.Task;
            yield return new TestStep(_ =>
            {
                stepExecuted = true;
                return Task.CompletedTask;
            }, ServiceProvider);
        }
    }

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

        await productionGate.Task;

        cts.Cancel();
        cancelGate.SetResult(true);

        await runTask;

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

                cts.Token.ThrowIfCancellationRequested();

                producedSteps.Add(i);
                yield return new TestStep(_ => Task.CompletedTask, ServiceProvider);
            }
        }
    }

    #endregion

    #region PrepareAsync

    [Fact]
    public async Task PrepareAsync_ConcurrentCallsDuringStepProduction_OnlyFirstSucceeds()
    {
        var productionStarted = new TaskCompletionSource<bool>();
        var continueProduction = new TaskCompletionSource<bool>();
        var prepareCount = 0;

        var pipeline = CreateConsumerPipeline(ProduceStepsAsync());

        var firstTask = pipeline.PrepareAsync(CancellationToken.None);

        await productionStarted.Task;

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.PrepareAsync(CancellationToken.None));

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

        var runTask = pipeline.RunAsync(CancellationToken.None);

        await preparationStarted.Task;

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.PrepareAsync(CancellationToken.None));

        continuePreparation.SetResult(true);
        await runTask;

        Assert.False(pipeline.Failed);

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

    #region Dispose

    [Fact]
    public async Task Dispose_DisposesRunner()
    {
        var pipeline = CreateStepRunnerPipelineBase([]);
        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);
        pipeline.Dispose();
        Assert.True(pipeline.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => pipeline.StepRunner.AddStep(new TestStep(null, ServiceProvider)));
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