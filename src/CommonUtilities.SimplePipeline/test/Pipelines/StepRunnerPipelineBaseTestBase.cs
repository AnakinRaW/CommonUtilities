using AnakinRaW.CommonUtilities.Testing.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public abstract class StepRunnerPipelineBaseTestBase<TRunner> : PipelineTestBase where TRunner : class, IStepRunner
{
    protected virtual bool RunnerSupportsSequentialRuns => true;

    protected virtual bool RunnerSupportsConcurrentRuns => true;

    protected abstract StepRunnerPipelineBase<TRunner> CreateStepRunnerPipelineBase(
        IList<IStep> steps, 
        bool failFast, 
        RunnerBehavior runnerBehavior);

    protected StepRunnerPipelineBase<TRunner> CreateStepRunnerPipelineBase(IList<IStep> steps)
    {
        var sequential = GetRandomRunBehavior();
        return CreateStepRunnerPipelineBase(steps, Random.Bool(), sequential);
    }

    protected StepRunnerPipelineBase<TRunner> CreateStepRunnerPipelineBase(IList<IStep> steps, bool failFast)
    {
        var sequential = GetRandomRunBehavior();
        return CreateStepRunnerPipelineBase(steps, failFast, sequential);
    }

    protected override Pipeline CreatePipeline(IList<IStep> steps)
    {
        return CreateStepRunnerPipelineBase(steps, false);
    }

    protected RunnerBehavior GetRandomRunBehavior()
    {
        if (RunnerSupportsConcurrentRuns && RunnerSupportsSequentialRuns)
            return Random.Enum<RunnerBehavior>();
        if (RunnerSupportsConcurrentRuns)
            return RunnerBehavior.Concurrent;
        if (RunnerSupportsSequentialRuns) 
            return RunnerBehavior.Sequential;
        throw new NotSupportedException();
    }

    protected virtual int GetWorkerCount(RunnerBehavior runnerBehavior)
    {
        return runnerBehavior is RunnerBehavior.Sequential ? 1 : 4;
    }

    protected bool IsRunBehaviorSupported(RunnerBehavior runnerBehavior)
    {
        switch (runnerBehavior)
        {
            case RunnerBehavior.Sequential when !RunnerSupportsSequentialRuns:
            case RunnerBehavior.Concurrent when !RunnerSupportsConcurrentRuns:
                return false;
        }

        return true;
    }

    #region FailFast

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailFast_CtorSetsProperty(bool failFast)
    {
        var pipeline = CreateStepRunnerPipelineBase([], failFast);
        Assert.Equal(failFast, pipeline.FailFast);
    }

    #endregion

    #region StepRunner

    [Fact]
    public void StepRunner_AccessAlwaysInitializes()
    {
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        var pipeline = CreateStepRunnerPipelineBase([step], Random.Bool());
        Assert.NotNull(pipeline.StepRunner);
    }

    [Fact]
    public async Task StepRunner_IsNotNullAfterPreparation()
    {
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        var pipeline = CreateStepRunnerPipelineBase([step], Random.Bool());

        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(pipeline.StepRunner);
    }

    #endregion

    #region IsStepRunnerInitialized

    [Fact]
    public async Task IsStepRunnerInitialized_IsInitializedAfterPrepare()
    {
        var pipeline = CreateStepRunnerPipelineBase([], Random.Bool());
        Assert.False(pipeline.IsStepRunnerInitialized);
        await pipeline.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.True(pipeline.IsStepRunnerInitialized);
    }

    [Fact]
    public async Task IsStepRunnerInitialized_IsInitializedAfterRun()
    {
        var pipeline = CreateStepRunnerPipelineBase([], Random.Bool());
        Assert.False(pipeline.IsStepRunnerInitialized);
        await pipeline.RunAsync(TestContext.Current.CancellationToken);
        Assert.True(pipeline.IsStepRunnerInitialized);
    }

    #endregion

    #region RunAsync

    [Fact]
    public async Task RunAsync_EmptyPipeline_Succeeds()
    {
        var pipeline = CreateStepRunnerPipelineBase([]);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.False(pipeline.Failed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_AllStepsExecuted(bool prepare)
    {
        var runCounter = 0;

        var s1 = new TestStep(_ =>
        {
            Interlocked.Increment(ref runCounter);
            return Task.CompletedTask;
        }, ServiceProvider);
        var s2 = new TestStep(_ =>
        {
            Interlocked.Increment(ref runCounter);
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = CreateStepRunnerPipelineBase([s1, s2]);

        if (prepare)
            await pipeline.PrepareAsync(TestContext.Current.CancellationToken);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, runCounter);
        Assert.False(pipeline.Failed);
    }

    [Fact]
    public async Task RunAsync_StepReceivesValidCancellationToken()
    {
        var receivedToken = CancellationToken.None;
        var tokenWasProvided = false;

        var step = new TestStep(ct =>
        {
            receivedToken = ct;
            tokenWasProvided = ct.CanBeCanceled;
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = CreateStepRunnerPipelineBase([step]);

        using var cts = new CancellationTokenSource();
        await pipeline.RunAsync(cts.Token);

        Assert.True(tokenWasProvided);
        Assert.True(receivedToken.CanBeCanceled);
    }

    [Theory]
    [InlineData(RunnerBehavior.Sequential)]
    [InlineData(RunnerBehavior.Concurrent)]
    public async Task RunAsync_FailFastEnabled_StepThrows_StopsExecution(RunnerBehavior runnerBehavior)
    {
        if (!IsRunBehaviorSupported(runnerBehavior))
            return;

        var secondStepRan = false;
        var s1 = new TestStep(_ => throw new Exception("Test"), ServiceProvider);
        var s2 = new TestStep(_ =>
        {
            secondStepRan = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        var pipeline = CreateStepRunnerPipelineBase([s1, s2], true, runnerBehavior);

        await Assert.ThrowsAsync<StepFailureException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.Failed);

        if (runnerBehavior is RunnerBehavior.Sequential)
        {
            Assert.False(secondStepRan, "FailFast should prevent subsequent steps from running");
            Assert.True(pipeline.Cancelled);
        }
    }

    [Theory]
    [InlineData(RunnerBehavior.Sequential)]
    [InlineData(RunnerBehavior.Concurrent)]
    public async Task RunAsync_FailFastDisabled_ContinuesAfterError(RunnerBehavior runnerBehavior)
    {
        if (!IsRunBehaviorSupported(runnerBehavior))
            return;

        var executed = new ConcurrentQueue<int>();

        var s1 = new TestStep(_ => { executed.Enqueue(1); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(_ => { executed.Enqueue(2); throw new InvalidOperationException(); }, ServiceProvider);
        var s3 = new TestStep(_ => { executed.Enqueue(3); return Task.CompletedTask; }, ServiceProvider);

        var pipeline = CreateStepRunnerPipelineBase([s1, s2, s3], false, runnerBehavior);

        await Assert.ThrowsAsync<StepFailureException>(
            () => pipeline.RunAsync(TestContext.Current.CancellationToken));

        if (runnerBehavior is RunnerBehavior.Sequential)
            Assert.Equal([1, 2, 3], executed);
        else
            Assert.EqualUnordered([1, 2, 3], executed.ToList());
    }

    [Fact]
    public async Task RunAsync_StepThrowsException_ThrowsStepFailureException()
    {
        var step = new TestStep(_ => throw new InvalidOperationException("Test error"), ServiceProvider);
        var pipeline = CreatePipeline([step]);

        var e  = await Assert.ThrowsAsync<StepFailureException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.Failed);
        Assert.False(pipeline.Cancelled);
        Assert.Contains("failed with error", e.Message);
        Assert.Contains("Test error", e.Message);
    }

    [Fact]
    public async Task RunAsync_MultipleStepsThrow_AllErrorsCaptured()
    {
        var s1 = new TestStep(_ => throw new Exception("Error1"), ServiceProvider);
        var s2 = new TestStep(_ => throw new Exception("Error2"), ServiceProvider);

        var pipeline = CreateStepRunnerPipelineBase([s1, s2], false);

        var e = await Assert.ThrowsAsync<StepFailureException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.Failed);
        Assert.Contains("failed with error", e.Message);
        Assert.Contains("Error1", e.Message);
        Assert.Contains("Error2", e.Message);
    }

    [Fact]
    public async Task RunAsync_WithPreCancelledToken_ThrowsAndSetsPipelineCancelled()
    {
        var executed = false;
        var step = new TestStep(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);
        var pipeline = CreatePipeline([step]);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.RunAsync(cts.Token));

        Assert.False(executed);
        Assert.True(pipeline.Cancelled);
        Assert.False(pipeline.Failed);
    }

    [Fact]
    public async Task RunAsync_StepThrowsOperationCanceledException_TreatedAsCancellation()
    {
        var s1 = new TestStep(_ => throw new OperationCanceledException(), ServiceProvider);

        var pipeline = CreatePipeline([s1]);

        // OperationCanceledException from a step should propagate
        await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.True(pipeline.Cancelled);
        Assert.False(pipeline.Failed);
    }

    [Fact]
    public async Task RunAsync_StepThrowsAggregateExceptionWithCancellation_TreatedAsCancellation()
    {
        var step = new TestStep(_ => throw new AggregateException(new OperationCanceledException()), ServiceProvider);

        var pipeline = CreatePipeline([step]);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            pipeline.RunAsync(TestContext.Current.CancellationToken));

        Assert.False(pipeline.Failed);
        Assert.True(pipeline.Cancelled);
    }

    [Fact]
    public async Task RunAsync_Concurrent_ExecutesStepsInParallel()
    {
        if (!RunnerSupportsConcurrentRuns)
            return;

        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        var steps = new List<IStep>();
        for (var i = 0; i < GetWorkerCount(RunnerBehavior.Concurrent) * 10; i++)
        {
            steps.Add(new TestStep(async _ =>
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
            }, ServiceProvider));
        }

        var pipeline = CreateStepRunnerPipelineBase(steps, Random.Bool(), RunnerBehavior.Concurrent);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(maxConcurrent > 1, $"Expected parallel execution, but max concurrent was {maxConcurrent}");
    }

    [Fact]
    public async Task RunAsync_Concurrent_RespectsWorkerCount()
    {
        if (!RunnerSupportsConcurrentRuns)
            return;

        var executedSteps = new ConcurrentBag<int>();
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        var steps = new List<IStep>();

        var stepCount = GetWorkerCount(RunnerBehavior.Concurrent) * 10;
        for (var i = 1; i <= stepCount; i++)
        {
            var i1 = i;
            steps.Add(new TestStep(async _ =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }

                await Task.Delay(new Random().Next(50, 300), TestContext.Current.CancellationToken);
                executedSteps.Add(i1);

                lock (lockObj)
                {
                    concurrentCount--;
                }
            }, ServiceProvider));
        }

        var pipeline = CreateStepRunnerPipelineBase(steps, Random.Bool(), RunnerBehavior.Concurrent);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(maxConcurrent <= pipeline.StepRunner.WorkerCount, 
            $"Expected max {pipeline.StepRunner.WorkerCount} concurrent, but was {maxConcurrent}");

        Assert.Equal(stepCount, executedSteps.Count);
        Assert.Equal(stepCount * (stepCount + 1) / 2, executedSteps.Sum(x => x));
    }

    [Fact]
    public async Task RunAsync_Sequential_MultipleSteps()
    {
        if (!RunnerSupportsSequentialRuns)
            return;

        var sb = new StringBuilder();

        var s1 = new TestStep(_ => { sb.Append('a'); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(async _ =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            sb.Append('b');
        }, ServiceProvider);
        var s3 = new TestStep(_ => { sb.Append('c'); return Task.CompletedTask; }, ServiceProvider);

        var pipeline = CreateStepRunnerPipelineBase([s1, s2, s3], Random.Bool(), RunnerBehavior.Sequential);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal("abc", sb.ToString());
        Assert.False(pipeline.Failed);
    }

    [Theory]
    [InlineData(true, RunnerBehavior.Concurrent)]
    [InlineData(true, RunnerBehavior.Sequential)]
    [InlineData(false, RunnerBehavior.Concurrent)]
    [InlineData(false, RunnerBehavior.Sequential)]
    public async Task RunAsync_CancelledMidSequence_StopsOnFailFast(bool failFast, RunnerBehavior runnerBehavior)
    {
        if (!IsRunBehaviorSupported(runnerBehavior))
            return;

        using var cts = new CancellationTokenSource();
        var executedSteps = new ConcurrentQueue<int>();

        var s1 = new TestStep(_ => { executedSteps.Enqueue(1); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(_ =>
        {
            executedSteps.Enqueue(2);
            cts.Cancel();
            return Task.CompletedTask;
        }, ServiceProvider);
        var s3 = new TestStep(_ => { executedSteps.Enqueue(3); return Task.CompletedTask; }, ServiceProvider);

        var pipeline = CreateStepRunnerPipelineBase([s1, s2, s3], failFast, runnerBehavior);

        await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.RunAsync(cts.Token));

        if (runnerBehavior is RunnerBehavior.Sequential)
            Assert.Equal([1, 2], executedSteps);
        else
            Assert.Contains(2, executedSteps);
    }

    #endregion

    #region Common Usage Tests

    [Theory]
    [InlineData(RunnerBehavior.Concurrent)]
    [InlineData(RunnerBehavior.Sequential)]
    public async Task UsageTest_StepsWaitingForEachOther(RunnerBehavior runnerBehavior)
    {
        if (!IsRunBehaviorSupported(runnerBehavior))
            return;

        var executedSteps = new ConcurrentQueue<int>();

        var s1 = new TestStep(async _ =>
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
            executedSteps.Enqueue(1);
        }, ServiceProvider);
        var s2 = new TestStep(async _ =>
        {
            await s1;
            executedSteps.Enqueue(2);
        }, ServiceProvider);
        var s3 = new TestStep(_ =>
        {
            executedSteps.Enqueue(3);
            return Task.CompletedTask;
        }, ServiceProvider);


        var pipeline = CreateStepRunnerPipelineBase([s1, s2, s3], Random.Bool(), runnerBehavior);

        await pipeline.RunAsync(TestContext.Current.CancellationToken);

        if (runnerBehavior == RunnerBehavior.Sequential)
            Assert.Equal([1,2,3], executedSteps);
        else
        {
            var list = executedSteps.ToList();
            Assert.True(list.IndexOf(1) < list.IndexOf(2), "1 should appear before 2");
        }
    }

    [Theory]
    [InlineData(RunnerBehavior.Concurrent)]
    [InlineData(RunnerBehavior.Sequential)]
    public async Task UsageTest_StepsWaitingForEachOther_WrongInsertionOrderHangsOnSequential_WorksForConcurrentRun(RunnerBehavior runnerBehavior)
    {
        if (!IsRunBehaviorSupported(runnerBehavior))
            return;

        var executedSteps = new ConcurrentQueue<int>();

        var s1 = new TestStep(_ =>
        {
            executedSteps.Enqueue(1);
            return Task.CompletedTask;
        }, ServiceProvider);
        var s2 = new TestStep(async _ =>
        {
            await s1;
            executedSteps.Enqueue(2);
        }, ServiceProvider);
        var s3 = new TestStep(_ =>
        {
            executedSteps.Enqueue(3);
            return Task.CompletedTask;
        }, ServiceProvider);

        // Insertion order for Sequential runs is broken and will cause starvation
        var pipeline = CreateStepRunnerPipelineBase([s2, s1, s3], Random.Bool(), runnerBehavior);
        
        if (runnerBehavior == RunnerBehavior.Sequential)
        {
            var runTask = pipeline.RunAsync(TestContext.Current.CancellationToken);
            var finished = await Task.WhenAny(
                runTask, 
                Task.Delay(5000, TestContext.Current.CancellationToken));
            Assert.NotEqual(runTask, finished);
        }
        else
        {
            await pipeline.RunAsync(TestContext.Current.CancellationToken);
            var list = executedSteps.ToList();
            Assert.True(list.IndexOf(1) < list.IndexOf(2), "1 should appear before 2");
        }
    }

    #endregion
}