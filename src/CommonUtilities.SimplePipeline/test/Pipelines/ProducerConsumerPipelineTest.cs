using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using AnakinRaW.CommonUtilities.Testing.Extensions;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class ProducerConsumerPipelineTest : StepRunnerPipelineBaseTestSuite<ProducerConsumerStepRunner>
{
    protected override StepRunnerPipelineBase<ProducerConsumerStepRunner> CreateStepRunnerPipelineBase(IList<IStep> steps, bool failFast, RunnerBehavior runnerBehavior)
    {
        return CreateConsumerPipeline(steps.ToAsyncEnumerable(), failFast, runnerBehavior);
    }

    protected override ITrackingPipeline CreateTrackingPipeline(Func<CancellationToken, Task> prepare, Func<CancellationToken, Task> run)
    {
        IEnumerable<IStep> steps = [new TestStep(run, ServiceProvider)];
        return new TestProducerConsumerPipeline(ServiceProvider, steps.ToAsyncEnumerable(), prepare, GetWorkerCount(GetRandomRunBehavior()), false);
    }

    protected override StepRunnerPipelineBase<ProducerConsumerStepRunner> CreateTrackingPipeline(IList<IStep> steps, bool failFast, RunnerBehavior runnerBehavior, List<string> callOrder, string? throwOnMethod = null)
    {
        return new TrackingProducerConsumerPipeline(ServiceProvider, steps.ToAsyncEnumerable(), GetWorkerCount(runnerBehavior), failFast, callOrder, throwOnMethod);
    }

    private ProducerConsumerPipeline CreateConsumerPipeline(IAsyncEnumerable<IStep> steps, bool failFast, RunnerBehavior runnerBehavior)
    {
        return new TestProducerConsumerPipeline(ServiceProvider, steps, null, GetWorkerCount(runnerBehavior), failFast);
    }

    private ProducerConsumerPipeline CreateConsumerPipeline(IAsyncEnumerable<IStep> steps)
    {
        return CreateConsumerPipeline(steps, Random.Bool(), GetRandomRunBehavior());
    }

    #region Constructor Tests

    [Fact]
    public void Ctor_NullServiceProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestProducerConsumerPipeline(
            null!, AsyncEnumerable.Empty<IStep>(), null, 4, true));
    }

    [Fact]
    public void Ctor_InvalidWorkerCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestProducerConsumerPipeline(
            ServiceProvider, AsyncEnumerable.Empty<IStep>(), null, 0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestProducerConsumerPipeline(
            ServiceProvider, AsyncEnumerable.Empty<IStep>(), null, new Random().Next(int.MinValue, 0), true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestProducerConsumerPipeline(
            ServiceProvider, AsyncEnumerable.Empty<IStep>(), null, new Random().Next(65, int.MaxValue), true));
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
        return;

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
        var cts = new CancellationTokenSource();
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

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pipeline.RunAsync(cts.Token));

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

        var runTask = Assert.ThrowsAnyAsync<OperationCanceledException>(() => pipeline.RunAsync(cts.Token));

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

    [Theory]
    [InlineData(RunnerBehavior.Concurrent)]
    [InlineData(RunnerBehavior.Sequential)]
    public async Task RunAsync_PreparationAndExecutionConcurrent_NoDeadlock(RunnerBehavior runnerBehavior)
    {
        var firstStepStarted = new ManualResetEventSlim(false);
        var allowPreparationToContinue = new ManualResetEventSlim(false);
        var executedSteps = new ConcurrentBag<int>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, TestContext.Current.CancellationToken);

        
        var pipeline = new NonAwaitingTestProducerConsumerPipeline(ServiceProvider,
            GetWorkerCount(runnerBehavior), buildSteps: BuildSteps);

        var runTask = pipeline.RunAsync(linkedCts.Token);

        // Wait for execution to start with timeout
        Assert.True(firstStepStarted.Wait(TimeSpan.FromSeconds(5), linkedCts.Token),
            "First step did not start; pipeline may be deadlocked.");

        // Allow preparation to continue producing steps
        allowPreparationToContinue.Set();

        // Should complete without deadlock
        await runTask;

        Assert.False(cts.IsCancellationRequested, "Pipeline hit timeout; possible deadlock.");
        Assert.Equal(11, executedSteps.Count);
        Assert.Contains(0, executedSteps);
        return;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async IAsyncEnumerable<IStep> BuildSteps([EnumeratorCancellation] CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // First step: starts execution, signals, then blocks
            yield return new TestStep(async _ =>
            {
                await Task.Yield();
                executedSteps.Add(0);
                firstStepStarted.Set();

                // Wait with timeout to avoid hanging
                if (!allowPreparationToContinue.Wait(TimeSpan.FromSeconds(8), linkedCts.Token))
                    throw new TimeoutException("First step timed out waiting for continuation signal");
            }, ServiceProvider);

            // Wait for first step to actually start executing with timeout
            if (!firstStepStarted.Wait(TimeSpan.FromSeconds(5), linkedCts.Token))
                throw new TimeoutException("BuildSteps timed out waiting for first step to start");

            // Produce more steps while first step is still running
            for (var i = 1; i <= 10; i++)
            {
                var index = i;
                yield return new TestStep(_ =>
                {
                    executedSteps.Add(index);
                    return Task.CompletedTask;
                }, ServiceProvider);
            }
        }
    }

    [Fact]
    public async Task RunAsync_CancellationDoesNotCauseStepAddedProductionThrowsInvalidOperationException()
    {
        var productionStarted = new TaskCompletionSource<bool>();
        var enumerationCompleted = new TaskCompletionSource<bool>();

        var pipeline = CreateConsumerPipeline(
            CreateSteps(CancellationToken.None),
            Random.Bool(),
            RunnerBehavior.Sequential);

        var pipelineTask = pipeline.RunAsync(new CancellationToken(true));

        await productionStarted.Task;

        var productionCompletedAndHandledTask = Task.Run(async () =>
        {
            await enumerationCompleted.Task;
            // Required, to ensure the ExceptionHandler of RunPreparationAsync had time to complete.
            await Task.Delay(200, CancellationToken.None);
        }, CancellationToken.None);

        var completed = await Task.WhenAny(productionCompletedAndHandledTask, Task.Delay(5000, CancellationToken.None));
        Assert.Equal(productionCompletedAndHandledTask, completed);

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pipelineTask);
        
        Assert.False(pipeline.Failed);
        return;

        async IAsyncEnumerable<IStep> CreateSteps([EnumeratorCancellation] CancellationToken _)
        {
            productionStarted.SetResult(true);
            try
            {
                yield return new TestStep(_ => Task.CompletedTask, ServiceProvider);
            }
            finally
            {
                enumerationCompleted.TrySetResult(true);
            }
        }
    }

    [Fact]
    public async Task RunAsync_StepAddedAfterRunnerFinishedUnexpectedly_RethrowsInvalidOperationException()
    {
        var productionStarted = new TaskCompletionSource<bool>();
        var proceedWithProduction = new TaskCompletionSource<bool>();
        var enumerationCompleted = new TaskCompletionSource<bool>();

        var pipeline = new TestProducerConsumerPipelineExposed(
            ServiceProvider,
            CreateSteps(CancellationToken.None),
            prepareAction: null,
            workerCount: 1,
            failFast: false);

        var pipelineTask = pipeline.RunAsync(CancellationToken.None);

        await productionStarted.Task;

        // Finish the runner without cancellation request,
        // which means that something really unexpected was going on
        pipeline.ExposedStepRunner.Finish();

        proceedWithProduction.SetResult(true);

        var productionCompletedAndHandledTask = Task.Run(async () =>
        {
            await enumerationCompleted.Task;
            // Required, to ensure the ExceptionHandler of RunPreparationAsync had time to complete.
            await Task.Delay(200, CancellationToken.None);
        }, CancellationToken.None);

        var completed = await Task.WhenAny(productionCompletedAndHandledTask, Task.Delay(5000, CancellationToken.None));
        Assert.Equal(productionCompletedAndHandledTask, completed);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipelineTask);

        Assert.True(pipeline.Failed);
        return;

        async IAsyncEnumerable<IStep> CreateSteps([EnumeratorCancellation] CancellationToken _)
        {
            productionStarted.SetResult(true);
            await proceedWithProduction.Task;

            try
            {
                yield return new TestStep(_ => Task.CompletedTask, ServiceProvider);
            }
            finally
            {
                enumerationCompleted.TrySetResult(true);
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
    public async Task PrepareAsync_RunnerInitializedWithCtorWorkerCount()
    {
        var workerCount = new Random().Next(1, 65);
        var pipeline = new TestProducerConsumerPipeline(ServiceProvider, Array.Empty<IStep>().ToAsyncEnumerable(),
            null, workerCount, false);

        await pipeline.PrepareAsync(CancellationToken.None);

        Assert.Equal(workerCount, pipeline.StepRunner.WorkerCount);
    }

    #endregion

    private class TestProducerConsumerPipeline : ProducerConsumerPipeline, ITrackingPipeline
    {
        private readonly IAsyncEnumerable<IStep> _steps;
        private readonly Func<CancellationToken, Task>? _prepareAction;

        public TestProducerConsumerPipeline(
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
                //token.ThrowIfCancellationRequested();
                yield return step;
            }
        }
    }

    private class NonAwaitingTestProducerConsumerPipeline(
        IServiceProvider serviceProvider,
        int workerCount,
        Func<CancellationToken, IAsyncEnumerable<IStep>> buildSteps)
        : ProducerConsumerPipeline(workerCount, serviceProvider)
    {
        private readonly Func<CancellationToken, IAsyncEnumerable<IStep>> _buildSteps = buildSteps ?? throw new ArgumentNullException(nameof(buildSteps));

        protected override IAsyncEnumerable<IStep> BuildStepsAsync(CancellationToken token)
        {
            return _buildSteps(token);
        }
    }

    private class TestProducerConsumerPipelineExposed(
        IServiceProvider serviceProvider,
        IAsyncEnumerable<IStep> steps,
        Func<CancellationToken, Task>? prepareAction,
        int workerCount,
        bool failFast)
        : TestProducerConsumerPipeline(serviceProvider, steps, prepareAction, workerCount, failFast)
    {
        public ProducerConsumerStepRunner ExposedStepRunner => StepRunner;
    }

    private class TrackingProducerConsumerPipeline : ProducerConsumerPipeline
    {
        private readonly IAsyncEnumerable<IStep> _steps;
        private readonly TrackingPipelineHelper _helper;

        public TrackingProducerConsumerPipeline(
            IServiceProvider serviceProvider,
            IAsyncEnumerable<IStep> steps,
            int workerCount,
            bool failFast,
            List<string> callOrder,
            string? throwOnMethod)
            : base(workerCount, serviceProvider)
        {
            _steps = steps;
            _helper = new TrackingPipelineHelper(callOrder, throwOnMethod);
            FailFast = failFast;
        }

        protected override IAsyncEnumerable<IStep> BuildStepsAsync(CancellationToken token)
        {
            return _steps;
        }

        protected override void OnExecuteStarted() => _helper.OnExecuteStarted();
        protected override void OnRunnerExecuted() => _helper.OnRunnerExecuted();
        protected override void OnExecuteCompleted() => _helper.OnExecuteCompleted();
    }
}