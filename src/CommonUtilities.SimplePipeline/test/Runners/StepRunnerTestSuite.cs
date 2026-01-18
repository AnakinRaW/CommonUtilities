using AnakinRaW.CommonUtilities.Testing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public abstract class StepRunnerTestSuite<T> : TestBaseWithServiceProvider where T : AsyncStepRunner
{
    /// <summary>
    /// Indicates whether the runner guarantees sequential step execution order.
    /// </summary>
    public virtual bool HasSequentialStepExecutionOrder => false;

    /// <summary>
    /// Indicates whether the runner supports sequential execution mode.
    /// </summary>
    public virtual bool SupportsSequentialExecutionOrder => true;

    protected virtual bool SupportsAddingStepsAfterCancellation => true;

    protected abstract T CreateStepRunner(bool? sequential = null);

    protected abstract T CreateStepRunner(int workerCount);

    protected virtual void FinishAdding(T runner)
    {
    }

    #region Initial State Tests

    [Fact]
    public void NewRunner_InitialState_ConcurrentRunner()
    {
        if (HasSequentialStepExecutionOrder)
            return;
        Assert.Throws<ArgumentOutOfRangeException>("workerCount", () => CreateStepRunner(0));
        Assert.Throws<ArgumentOutOfRangeException>("workerCount", () => CreateStepRunner(new Random().Next(int.MinValue, 0)));
        Assert.Throws<ArgumentOutOfRangeException>("workerCount", () => CreateStepRunner(new Random().Next(65, int.MaxValue)));

        var workerCount = new Random().Next(2, 65);
        var runner = CreateStepRunner(workerCount);
        Assert.Equal(workerCount, runner.WorkerCount);
        Assert.False(runner.IsSequential);
    }

    [Fact]
    public void NewRunner_InitialState_Sequential()
    {
        if (!SupportsSequentialExecutionOrder)
            return;

        var runner = CreateStepRunner(true);
        Assert.Equal(1, runner.WorkerCount);
        Assert.True(runner.IsSequential);
    }

    [Fact]
    public void NewRunner_InitialState_IsCorrect()
    {
        var runner = CreateStepRunner();

        Assert.Empty(runner.ExecutedSteps);
        Assert.Null(runner.Exception);
        Assert.True(runner.WorkerCount >= 1);
    }

    [Fact]
    public void NewRunner_Sequential_WorkerCountIsOne()
    {
        if (!SupportsSequentialExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: true);
        Assert.Equal(1, runner.WorkerCount);
    }

    #endregion

    #region IsRunning Property Tests

    [Fact]
    public void IsRunning_BeforeRun_ReturnsFalse()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        runner.AddStep(step);

        Assert.False(runner.IsRunning);
    }

    [Fact]
    public async Task IsRunning_DuringExecution_ReturnsTrue()
    {
        var runner = CreateStepRunner();
        var isRunningDuringExecution = false;
        var stepStarted = new ManualResetEventSlim(false);
        var canComplete = new ManualResetEventSlim(false);

        var step = new TestStep(async _ =>
        {
            await Task.Yield();
            stepStarted.Set();
            isRunningDuringExecution = runner.IsRunning;
            canComplete.Wait(TestContext.Current.CancellationToken);
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var runTask = runner.RunAsync(CancellationToken.None);

        stepStarted.Wait(TestContext.Current.CancellationToken);
        Assert.True(runner.IsRunning, "IsRunning should be true during execution");

        canComplete.Set();
        await runTask;

        Assert.True(isRunningDuringExecution, "IsRunning should have been true inside step");
    }

    [Fact]
    public async Task IsRunning_AfterSuccessfulCompletion_ReturnsFalse()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.False(runner.IsRunning);
    }

    [Fact]
    public async Task IsRunning_AfterExecutionWithErrors_ReturnsFalse()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => throw new InvalidOperationException(), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.False(runner.IsRunning);
    }

    [Fact]
    public async Task IsRunning_AfterCancellation_ReturnsFalse()
    {
        var runner = CreateStepRunner();
        var cts = new CancellationTokenSource();
        var stepStarted = new ManualResetEventSlim(false);
        var canComplete = new ManualResetEventSlim(false);

        var step = new TestStep(async _ =>
        {
            await Task.Yield();
            stepStarted.Set();
            canComplete.Wait(TestContext.Current.CancellationToken);
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var runTask = runner.RunAsync(cts.Token);

        stepStarted.Wait(TestContext.Current.CancellationToken);
        cts.Cancel();
        canComplete.Set();

        await runTask;

        Assert.False(runner.IsRunning);
    }

    #endregion

    #region IsCancelled Property Tests

    [Fact]
    public void IsCancelled_BeforeRun_ReturnsFalse()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        runner.AddStep(step);

        Assert.False(runner.IsCancelled);
    }

    [Fact]
    public async Task IsCancelled_DuringNormalExecution_ReturnsFalse()
    {
        var runner = CreateStepRunner();
        var isCancelledDuringExecution = false;
        var stepStarted = new ManualResetEventSlim(false);
        var canComplete = new ManualResetEventSlim(false);

        var step = new TestStep(async _ =>
        {
            await Task.Yield();
            stepStarted.Set();
            isCancelledDuringExecution = runner.IsCancelled;
            canComplete.Wait(TestContext.Current.CancellationToken);
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var runTask = runner.RunAsync(CancellationToken.None);

        stepStarted.Wait(TestContext.Current.CancellationToken);
        Assert.False(runner.IsCancelled, "IsCancelled should be false during normal execution");

        canComplete.Set();
        await runTask;

        Assert.False(isCancelledDuringExecution, "IsCancelled should have been false inside step");
    }

    [Fact]
    public async Task IsCancelled_AfterNormalCompletion_ReturnsFalse()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.False(runner.IsCancelled);
    }

    [Fact]
    public async Task IsCancelled_AfterCancellation_ReturnsTrue()
    {
        var runner = CreateStepRunner();
        var cts = new CancellationTokenSource();
        var stepStarted = new ManualResetEventSlim(false);
        var canComplete = new ManualResetEventSlim(false);

        var step = new TestStep(async ct =>
        {
            await Task.Yield();
            stepStarted.Set();
            canComplete.Wait(TestContext.Current.CancellationToken);
            ct.ThrowIfCancellationRequested();
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var runTask = runner.RunAsync(cts.Token);

        stepStarted.Wait(TestContext.Current.CancellationToken);
        cts.Cancel();
        canComplete.Set();

        await runTask;

        Assert.True(runner.IsCancelled);
    }

    [Fact]
    public async Task IsCancelled_AfterExecutionWithErrors_ReturnsFalse()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => throw new InvalidOperationException(), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.False(runner.IsCancelled, "IsCancelled should be false when errors occur without cancellation");
        Assert.NotNull(runner.Exception);
    }

    [Fact]
    public async Task IsCancelled_AlreadyCancelledToken_ReturnsTrue()
    {
        var runner = CreateStepRunner();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(cts.Token);

        Assert.True(runner.IsCancelled);
    }

    [Fact]
    public async Task IsCancelled_StopRunnerExceptionWithCancel_ReturnsTrue()
    {
        var runner = CreateStepRunner();
        
        var step = new TestStep(_ => throw new StopRunnerException(), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.True(runner.IsCancelled, "IsCancelled should be true when StopRunnerException causes cancellation");
    }

    [Fact]
    public async Task IsCancelled_OnErrorSetsCancellation_ReturnsTrue()
    {
        var runner = CreateStepRunner();

        runner.Error += (_, args) =>
        { 
            args.Cancel = true;
        };

        var step = new TestStep(_ => throw new Exception(), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.True(runner.IsCancelled, "IsCancelled should be true when StopRunnerException causes cancellation");
    }

    #endregion

    #region AddStep Tests

    [Fact]
    public void AddStep_Null_ThrowsArgumentNullException()
    {
        var runner = CreateStepRunner();
        Assert.Throws<ArgumentNullException>(() => runner.AddStep(null!));
    }

    [Fact]
    public void AddStep_ValidStep_DoesNotThrow()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        var exception = Record.Exception(() => runner.AddStep(step));

        Assert.Null(exception);
    }

    [Fact]
    public void AddStep_MultipleSteps_AllAccepted()
    {
        var runner = CreateStepRunner();

        for (var i = 0; i < 10; i++)
        {
            var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
            runner.AddStep(step);
        }

        // Should not throw
    }

    [Fact]
    public async Task AddStep_DuringExecution_StepGetsExecuted()
    {
        var runner = CreateStepRunner();
        var executedSteps = new ConcurrentBag<string>();
        var gate = new ManualResetEventSlim(false);

        var step1 = new TestStep(async _ =>
        {
            executedSteps.Add("Step1");
            await Task.Yield();
            gate.Wait(TestContext.Current.CancellationToken);
        }, ServiceProvider);

        runner.AddStep(step1);

        var runTask = runner.RunAsync(CancellationToken.None);

        // Add step while runner is executing
        var step2 = new TestStep(_ =>
        {
            executedSteps.Add("Step2");
            return Task.CompletedTask;
        }, ServiceProvider);
        runner.AddStep(step2);

        gate.Set();
        FinishAdding(runner);

        await step2;
        await runTask;

        Assert.Contains("Step1", executedSteps);
        Assert.Contains("Step2", executedSteps);
    }

    #endregion

    #region RunAsync - Basic Execution Tests

    [Fact]
    public async Task RunAsync_NoSteps_CompletesSuccessfully()
    {
        var runner = CreateStepRunner();
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.Empty(runner.ExecutedSteps);
        Assert.Null(runner.Exception);
    }

    [Fact]
    public async Task RunAsync_SingleStep_ExecutesStep()
    {
        var runner = CreateStepRunner();
        var executed = false;
        var step = new TestStep(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.True(executed);
        Assert.Contains(step, runner.ExecutedSteps);
        await Assert.Single(runner.ExecutedSteps);
    }

    [Fact]
    public async Task RunAsync_MultipleSteps_ExecutesAllSteps()
    {
        var runner = CreateStepRunner();
        var executedSteps = new ConcurrentBag<string>();
        var executionOrder = new List<string>();
        var lockObj = new object();

        var step1 = new TestStep(_ =>
        {
            lock (lockObj) { executionOrder.Add("Step1"); }
            executedSteps.Add("Step1");
            return Task.CompletedTask;
        }, ServiceProvider);

        var step2 = new TestStep(_ =>
        {
            lock (lockObj) { executionOrder.Add("Step2"); }
            executedSteps.Add("Step2");
            return Task.CompletedTask;
        }, ServiceProvider);

        var step3 = new TestStep(_ =>
        {
            lock (lockObj) { executionOrder.Add("Step3"); }
            executedSteps.Add("Step3");
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        runner.AddStep(step3);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(3, executedSteps.Count);
        Assert.Contains("Step1", executedSteps);
        Assert.Contains("Step2", executedSteps);
        Assert.Contains("Step3", executedSteps);
        Assert.Equal(3, runner.ExecutedSteps.Count);

        if (HasSequentialStepExecutionOrder)
        {
            Assert.Equal(new[] { "Step1", "Step2", "Step3" }, executionOrder);
        }
    }

    [Fact]
    public async Task RunAsync_AsyncStep_WaitsForCompletion()
    {
        var runner = CreateStepRunner();
        var completed = false;

        var step = new TestStep(async _ =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            completed = true;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.True(completed);
    }

    [Fact]
    public async Task RunAsync_MixedSyncAndAsyncSteps_ExecutesAll()
    {
        var runner = CreateStepRunner();
        var executed = new ConcurrentBag<int>();

        runner.AddStep(new TestStep(_ => { executed.Add(1); return Task.CompletedTask; }, ServiceProvider));
        runner.AddStep(new TestStep(async _ => { await Task.Delay(10, TestContext.Current.CancellationToken); executed.Add(2); }, ServiceProvider));
        runner.AddStep(new TestStep(_ => { executed.Add(3); return Task.CompletedTask; }, ServiceProvider));
        runner.AddStep(new TestStep(async _ => { await Task.Yield(); executed.Add(4); }, ServiceProvider));

        FinishAdding(runner);
        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(4, executed.Count);
    }

    [Fact]
    public async Task RunAsync_SequentialRunner_ExecutesInAddOrder()
    {
        if (!SupportsSequentialExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: true);
        var executionOrder = new List<int>();

        for (var i = 0; i < 5; i++)
        {
            var index = i;
            var step = new TestStep(async _ =>
            {
                await Task.Yield();
                executionOrder.Add(index);
            }, ServiceProvider);
            runner.AddStep(step);
        }

        FinishAdding(runner);
        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, executionOrder);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunAsync_StepsAreExecutedOnThreadPool_DoesNotDeadlock(bool sequential)
    {
        if (sequential && !SupportsSequentialExecutionOrder)
            return;
        if (!sequential && HasSequentialStepExecutionOrder)
            return;

        var waitSource = new TaskCompletionSource<bool>();

        var runner = CreateStepRunner(sequential);
        var canComplete = new ManualResetEventSlim(false);

        var step1 = new TestStep(_ =>
        {
            canComplete.Wait();
            waitSource.SetResult(true);
            return Task.CompletedTask;
        }, ServiceProvider);
        var step2 = new TestStep(async _ =>
        {
            await waitSource.Task;
        }, ServiceProvider);


        runner.AddStep(step1);
        runner.AddStep(step2);

        FinishAdding(runner);

        var task = runner.RunAsync(CancellationToken.None);

        canComplete.Set();
        await waitSource.Task;

        var completedTask = 
            await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(task, completedTask);

        Assert.True(task is {IsCompleted: true, Status: TaskStatus.RanToCompletion});
    }

    #endregion

    #region RunAsync - Cancellation Tests

    [Fact]
    public async Task RunAsync_AlreadyCancelledToken_DoesNotExecuteAnyStep()
    {
        var runner = CreateStepRunner();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var executed = false;
        var step = new TestStep(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(cts.Token);

        Assert.False(executed);
        Assert.Empty(runner.ExecutedSteps);
    }

    [Fact]
    public async Task RunAsync_CancellationToken_IsCancellableAndLinkedToRunner()
    {
        var runner = CreateStepRunner();
        var cts = new CancellationTokenSource();
        var tokenWasCancellable = false;
        var tokenWasCancelledAfterRequest = false;

        var step = new TestStep(ct =>
        {
            tokenWasCancellable = ct.CanBeCanceled;
            cts.Cancel();
            tokenWasCancelledAfterRequest = ct.IsCancellationRequested;
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(cts.Token);

        Assert.True(tokenWasCancellable, "Token passed to step should be cancellable");
        Assert.True(tokenWasCancelledAfterRequest, "Token should reflect cancellation request");
    }

    [Fact]
    public async Task RunAsync_StepAddedAfterCancellation_IsNotExecuted()
    {
        if (!SupportsAddingStepsAfterCancellation)
            return;
        
        var runner = CreateStepRunner();
        var cts = new CancellationTokenSource();
        var step1Executed = false;
        var step2Executed = false;
        var barrier = new ManualResetEventSlim(false);
        var step1Started = new ManualResetEventSlim(false);

        var step1 = new TestStep(async _ =>
        {
            await Task.Yield();
            step1Started.Set();
            step1Executed = true;
            barrier.Wait(TestContext.Current.CancellationToken);
        }, ServiceProvider);

        runner.AddStep(step1);

        var runTask = runner.RunAsync(cts.Token);

        step1Started.Wait(TestContext.Current.CancellationToken);

        cts.Cancel();
        
        var step2 = new TestStep(_ =>
        {
            step2Executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);
        
        runner.AddStep(step2);
        
        barrier.Set();
        FinishAdding(runner);

        await runTask;

        Assert.True(step1Executed);
        Assert.False(step2Executed, "Step added after cancellation should not execute");
        Assert.Contains(step1, runner.ExecutedSteps);
        Assert.DoesNotContain(step2, runner.ExecutedSteps);
    }

    [Fact]
    public async Task RunAsync_SequentialRunner_CancellationStopsQueuedSteps()
    {
        if (!SupportsSequentialExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: true);
        var cts = new CancellationTokenSource();
        var stepsExecuted = new List<string>();

        var step1 = new TestStep(_ =>
        {
            stepsExecuted.Add("Step1");
            cts.Cancel();
            return Task.CompletedTask;
        }, ServiceProvider);

        var step2 = new TestStep(_ =>
        {
            stepsExecuted.Add("Step2");
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        await runner.RunAsync(cts.Token);

        Assert.Single(stepsExecuted);
        Assert.Equal("Step1", stepsExecuted[0]);
    }

    [Fact]
    public async Task RunAsync_StepThrowsOperationCanceledException_TreatedAsError()
    {
        var runner = CreateStepRunner();
        var errorRaised = false;
        runner.Error += (_, _) => errorRaised = true;

        var step = new TestStep(_ => throw new OperationCanceledException(), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.True(errorRaised);
        Assert.NotNull(runner.Exception);
    }

    [Fact]
    public async Task RunAsync_StepThrowsTaskCanceledException_TreatedAsError()
    {
        var runner = CreateStepRunner();
        var errorRaised = false;
        runner.Error += (_, _) => errorRaised = true;

        var step = new TestStep(_ => throw new TaskCanceledException(), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.True(errorRaised);
        Assert.NotNull(runner.Exception);
    }

    #endregion

    #region RunAsync - Error Handling Tests

    [Fact]
    public async Task RunAsync_StepThrows_OtherStepsStillExecute()
    {
        var runner = CreateStepRunner();
        var executedSteps = new ConcurrentBag<string>();
        var failingStepStarted = new ManualResetEventSlim(false);
        var failingStepCanThrow = new ManualResetEventSlim(false);

        var failingStep = new TestStep(async _ =>
        {
            await Task.Yield();
            executedSteps.Add("FailingStep");
            failingStepStarted.Set();
            failingStepCanThrow.Wait(TestContext.Current.CancellationToken);
            throw new InvalidOperationException("Test error");
        }, ServiceProvider);

        var successStep = new TestStep(_ =>
        {
            executedSteps.Add("SuccessStep");
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(failingStep);
        runner.AddStep(successStep);
        FinishAdding(runner);

        var runTask = runner.RunAsync(CancellationToken.None);

        failingStepStarted.Wait(TestContext.Current.CancellationToken);
        failingStepCanThrow.Set();

        await runTask;

        Assert.Contains("FailingStep", executedSteps);
        Assert.Contains("SuccessStep", executedSteps);
        Assert.Equal(2, runner.ExecutedSteps.Count);
        Assert.NotNull(runner.Exception);
    }

    [Fact]
    public async Task RunAsync_StepThrows_SetsStepErrorProperty()
    {
        var runner = CreateStepRunner();
        var expectedException = new InvalidOperationException("Test error");

        var step = new TestStep(_ => throw expectedException, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.Same(expectedException, step.Error);
    }

    [Fact]
    public async Task RunAsync_MultipleStepsThrow_AllErrorsRecorded()
    {
        var runner = CreateStepRunner();
        var exception1 = new InvalidOperationException("Error 1");
        var exception2 = new ArgumentException("Error 2");

        var step1 = new TestStep(_ => throw exception1, ServiceProvider);
        var step2 = new TestStep(_ => throw exception2, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(runner.Exception);
        Assert.Equal(2, runner.Exception.InnerExceptions.Count);
        Assert.Contains(exception1, runner.Exception.InnerExceptions);
        Assert.Contains(exception2, runner.Exception.InnerExceptions);
    }

    [Fact]
    public async Task RunAsync_ReentryNotAllowed_ThrowsInvalidOperationException()
    {
        var runner = CreateStepRunner();
        var mre = new ManualResetEventSlim(false);
        var isRunning = new TaskCompletionSource<bool>();
        
        var executed = false;
        var step = new TestStep(_ =>
        {
            isRunning.SetResult(true);
            mre.Wait();
            executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);


        Assert.False(runner.IsRunning);
        
        var runTask = runner.RunAsync(CancellationToken.None);

        await isRunning.Task;
        Assert.True(runner.IsRunning);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await runner.RunAsync(CancellationToken.None));
        mre.Set();

        await runTask;

        Assert.True(executed);
        Assert.Contains(step, runner.ExecutedSteps);
        await Assert.Single(runner.ExecutedSteps);
    }

    #endregion

    #region StopRunnerException Tests

    [Fact]
    public async Task RunAsync_StopRunnerException_StepAddedAfterException_IsNotExecuted()
    {
        var runner = CreateStepRunner();
        var step1Executed = false;
        var step2Executed = false;
        var errorOccurred = new ManualResetEventSlim(false);
        
        runner.Error += (_, args) =>
        {
            args.Cancel = true;

            if (args.Exception is StopRunnerException)
            {
                var step2 = new TestStep(_ =>
                {
                    step2Executed = true;
                    return Task.CompletedTask;
                }, ServiceProvider);
                runner.AddStep(step2);

                errorOccurred.Set();
            }
        };

        var step1 = new TestStep(async _ =>
        {
            await Task.Yield();
            step1Executed = true;
            throw new StopRunnerException();
        }, ServiceProvider);

        runner.AddStep(step1);

        var runTask = runner.RunAsync(CancellationToken.None);

        errorOccurred.Wait(TestContext.Current.CancellationToken);
       
        FinishAdding(runner);

        await runTask;

        Assert.True(step1Executed);
        Assert.False(step2Executed, "Step added before StopRunnerException should not execute");
        Assert.NotNull(runner.Exception);
        Assert.Contains(runner.Exception.InnerExceptions, e => e is StopRunnerException);
    }

    [Fact]
    public async Task RunAsync_StopRunnerException_SequentialRunner_StopsQueuedSteps()
    {
        if (!SupportsSequentialExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: true);
        var stepsExecuted = new List<string>();

        var step1 = new TestStep(_ =>
        {
            stepsExecuted.Add("Step1");
            throw new StopRunnerException();
        }, ServiceProvider);

        var step2 = new TestStep(_ =>
        {
            stepsExecuted.Add("Step2");
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.Single(stepsExecuted);
        Assert.Equal("Step1", stepsExecuted[0]);
    }

    [Fact]
    public async Task RunAsync_StepThrowsStopRunnerException_SetsCancelToTrue()
    {
        var runner = CreateStepRunner();
        StepRunnerErrorEventArgs? errorArgs = null;

        runner.Error += (_, args) =>
        {
            errorArgs = args;
        };

        var step = new TestStep(_ => throw new StopRunnerException(), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(errorArgs);
        Assert.IsType<StopRunnerException>(errorArgs.Exception);
        Assert.True(errorArgs.Cancel, "Cancel should be automatically set to true for StopRunnerException");
    }

    [Fact]
    public async Task RunAsync_StepThrowsStopRunnerException_ExceptionIsRecorded()
    {
        var runner = CreateStepRunner();
        var expectedException = new StopRunnerException();

        var step = new TestStep(_ => throw expectedException, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(runner.Exception);
        Assert.Contains(expectedException, runner.Exception.InnerExceptions);
    }

    #endregion

    #region Exception Property Tests

    [Fact]
    public void Exception_BeforeRun_ReturnsNull()
    {
        var runner = CreateStepRunner();
        runner.AddStep(new TestStep(_ => throw new Exception(), ServiceProvider));

        Assert.Null(runner.Exception);
    }

    [Fact]
    public async Task Exception_NoErrors_ReturnsNull()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.Null(runner.Exception);
    }

    [Fact]
    public async Task Exception_SingleError_ReturnsAggregateExceptionWithSingleInner()
    {
        var runner = CreateStepRunner();
        var expectedException = new InvalidOperationException("Test error");

        var step = new TestStep(_ => throw expectedException, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(runner.Exception);
        Assert.Single(runner.Exception.InnerExceptions);
        Assert.Same(expectedException, runner.Exception.InnerExceptions[0]);
    }

    [Fact]
    public async Task Exception_MultipleErrors_ReturnsAggregateExceptionWithAllErrors()
    {
        var runner = CreateStepRunner();
        var exception1 = new InvalidOperationException("Error 1");
        var exception2 = new ArgumentException("Error 2");
        var exception3 = new FormatException("Error 3");

        runner.AddStep(new TestStep(_ => throw exception1, ServiceProvider));
        runner.AddStep(new TestStep(_ => throw exception2, ServiceProvider));
        runner.AddStep(new TestStep(_ => throw exception3, ServiceProvider));
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(runner.Exception);
        Assert.Equal(3, runner.Exception.InnerExceptions.Count);
        Assert.Contains(exception1, runner.Exception.InnerExceptions);
        Assert.Contains(exception2, runner.Exception.InnerExceptions);
        Assert.Contains(exception3, runner.Exception.InnerExceptions);
    }

    #endregion

    #region Error Event Tests

    [Fact]
    public async Task Error_StepThrows_EventIsRaisedWithCorrectArgs()
    {
        var runner = CreateStepRunner();
        StepRunnerErrorEventArgs? errorArgs = null;
        object? sender = null;

        runner.Error += (s, args) =>
        {
            sender = s;
            errorArgs = args;
        };

        var expectedException = new InvalidOperationException("Test error");
        var step = new TestStep(_ => throw expectedException, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(errorArgs);
        Assert.Same(runner, sender);
        Assert.Same(step, errorArgs.Step);
        Assert.Same(expectedException, errorArgs.Exception);
        Assert.False(errorArgs.Cancel);
    }

    [Fact]
    public async Task Error_MultipleStepsThrow_EventIsRaisedForEachError()
    {
        var runner = CreateStepRunner();
        var errorCount = 0;
        var raisedSteps = new ConcurrentBag<IStep>();

        runner.Error += (_, args) =>
        {
            Interlocked.Increment(ref errorCount);
            raisedSteps.Add(args.Step!);
        };

        var step1 = new TestStep(_ => throw new Exception("Error 1"), ServiceProvider);
        var step2 = new TestStep(_ => throw new Exception("Error 2"), ServiceProvider);
        var step3 = new TestStep(_ => throw new Exception("Error 3"), ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        runner.AddStep(step3);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(3, errorCount);
        Assert.Contains(step1, raisedSteps);
        Assert.Contains(step2, raisedSteps);
        Assert.Contains(step3, raisedSteps);
    }

    [Fact]
    public async Task Error_SetCancelToTrue_StepAddedAfterError_IsNotExecuted()
    {
        var runner = CreateStepRunner();
        var step1Executed = false;
        var step2Executed = false;
        var errorOccurred = new ManualResetEventSlim(false);
        
        runner.Error += (_, args) =>
        {
            args.Cancel = true;
            errorOccurred.Set();
        };

        var step1 = new TestStep(async _ =>
        {
            await Task.Yield();
            step1Executed = true;
            throw new InvalidOperationException("Test error");
        }, ServiceProvider);

        runner.AddStep(step1);

        var runTask = runner.RunAsync(CancellationToken.None);

        errorOccurred.Wait(TestContext.Current.CancellationToken);
        
        // Give the runner time to process the error and cancel
        await Task.Delay(200, TestContext.Current.CancellationToken);
        
        var step2 = new TestStep(_ =>
        {
            step2Executed = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        if (SupportsAddingStepsAfterCancellation)
            runner.AddStep(step2);
        else
            Assert.Throws<InvalidOperationException>(() => runner.AddStep(step2));

        FinishAdding(runner);

        await runTask;

        if (SupportsAddingStepsAfterCancellation)
        {
            Assert.True(step1Executed);
            Assert.False(step2Executed, "Step added AFTER error with Cancel=true should not execute");
        }
        Assert.NotNull(runner.Exception);
    }

    [Fact]
    public async Task Error_SetCancelToTrue_SequentialRunner_StopsQueuedSteps()
    {
        if (!SupportsSequentialExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: true);
        var stepsExecuted = new List<string>();

        runner.Error += (_, args) =>
        {
            args.Cancel = true;
        };

        var step1 = new TestStep(_ =>
        {
            stepsExecuted.Add("Step1");
            throw new InvalidOperationException("Test error");
        }, ServiceProvider);

        var step2 = new TestStep(_ =>
        {
            stepsExecuted.Add("Step2");
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.Single(stepsExecuted);
        Assert.Equal("Step1", stepsExecuted[0]);
        Assert.NotNull(runner.Exception);
    }

    [Fact]
    public async Task Error_SuccessfulStep_EventIsNotRaised()
    {
        var runner = CreateStepRunner();
        var errorRaised = false;
        runner.Error += (_, _) => errorRaised = true;

        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.False(errorRaised);
    }

    [Fact]
    public async Task Error_NoSubscribers_DoesNotThrow()
    {
        var runner = CreateStepRunner();

        var step = new TestStep(_ => throw new Exception("Test"), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var exception = await Record.ExceptionAsync(() => runner.RunAsync(CancellationToken.None));

        Assert.Null(exception);
    }

    #endregion

    #region Wait() Tests

    [Fact]
    public async Task Wait_AfterSuccessfulRun_CompletesWithoutException()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        var exception = Record.Exception(() => runner.Wait());

        Assert.Null(exception);
    }

    [Fact]
    public async Task Wait_AfterFailedRun_ThrowsAggregateException()
    {
        var runner = CreateStepRunner();
        var expectedException = new InvalidOperationException("Test error");
        var step = new TestStep(_ => throw expectedException, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        var exception = Assert.Throws<AggregateException>(() => runner.Wait());
        Assert.Contains(expectedException, exception.InnerExceptions);
    }

    [Fact]
    public async Task Wait_DuringActiveRun_BlocksUntilCompletion()
    {
        var runner = CreateStepRunner();
        var stepCompleted = false;
        var tcs = new TaskCompletionSource<object?>();

        var step = new TestStep(async _ =>
        {
            await tcs.Task;
            stepCompleted = true;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        _ = runner.RunAsync(CancellationToken.None);

        var waitTask = Task.Run(() => runner.Wait(), TestContext.Current.CancellationToken);

        // Give Wait time to start blocking
        Thread.Sleep(100);
        Assert.False(waitTask.IsCompleted);

        // Complete the step
        tcs.SetResult(null);

        // Wait should complete now
        var completed = await WaitForTaskWithTimeout(waitTask, TimeSpan.FromSeconds(5));
        Assert.True(completed);
        Assert.True(stepCompleted);
    }

    [Fact]
    public async Task Wait_MultipleCallsAfterRun_AllSucceed()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        runner.Wait();
        runner.Wait();
        runner.Wait();
    }

    #endregion

    #region Wait(TimeSpan) Tests

    [Fact]
    public async Task WaitWithTimeout_CompletesInTime_DoesNotThrow()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        var exception = Record.Exception(() => runner.Wait(TimeSpan.FromSeconds(5)));

        Assert.Null(exception);
    }

    [Fact]
    public void WaitWithTimeout_TimeoutExpires_ThrowsTimeoutException()
    {
        var runner = CreateStepRunner();
        var tcs = new TaskCompletionSource<object?>();

        var step = new TestStep(async _ =>
        {
            await tcs.Task;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        _ = runner.RunAsync(CancellationToken.None);

        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromMilliseconds(100)));

        tcs.SetResult(null);
    }

    [Fact]
    public async Task WaitWithTimeout_StepFails_ThrowsAggregateException()
    {
        var runner = CreateStepRunner();
        var expectedException = new InvalidOperationException("Test error");
        var step = new TestStep(_ => throw expectedException, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        var exception = Assert.Throws<AggregateException>(() => runner.Wait(TimeSpan.FromSeconds(5)));
        Assert.Contains(expectedException, exception.InnerExceptions);
    }

    [Fact]
    public void WaitWithTimeout_ZeroTimeout_ThrowsTimeoutExceptionIfNotComplete()
    {
        var runner = CreateStepRunner();
        var tcs = new TaskCompletionSource<object?>();

        var step = new TestStep(async _ =>
        {
            await tcs.Task;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        _ = runner.RunAsync(CancellationToken.None);

        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.Zero));

        tcs.SetResult(null);
    }

    [Fact]
    public void WaitWithTimeout_NegativeTimeout_ThrowsArgumentOutOfRangeException()
    {
        var runner = CreateStepRunner();
        FinishAdding(runner);

        _ = runner.RunAsync(CancellationToken.None);

        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Wait(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void WaitWithTimeout_RunnerNeverRuns_ThrowsTimeoutException()
    {
        var runner = CreateStepRunner();
        FinishAdding(runner);
        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromSeconds(1)));
    }

    #endregion

    #region GetAwaiter / ConfigureAwait

    [Fact]
    public async Task GetAwaiter_BeforeRun_WaitsUntilRunnerStartedAndCompleted()
    {
        var runner = CreateStepRunner();
        var stepExecuted = false;

        var step = new TestStep(_ =>
        {
            stepExecuted = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var awaitTask = Task.Run(async () => await runner, TestContext.Current.CancellationToken);
        var awaitTaskConfigureAwaitT = Task.Run(async () => await runner.ConfigureAwait(true), TestContext.Current.CancellationToken);
        var awaitTaskConfigureAwaitF = Task.Run(async () => await runner.ConfigureAwait(false), TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaitTask.IsCompleted, "Awaiter should block until runner starts and completes");

        await runner.RunAsync(CancellationToken.None);

        await awaitTask;
        await awaitTaskConfigureAwaitT;
        await awaitTaskConfigureAwaitF;

        Assert.True(awaitTask.IsCompleted);
        Assert.True(awaitTaskConfigureAwaitT.IsCompleted);
        Assert.True(awaitTaskConfigureAwaitF.IsCompleted);
        Assert.True(stepExecuted);
    }

    [Fact]
    public async Task GetAwaiter_DuringRun_WaitsForCompletion()
    {
        var runner = CreateStepRunner();
        var stepCompleted = false;
        var tcs = new TaskCompletionSource<bool>();

        var step = new TestStep(async _ =>
        {
            await tcs.Task;
            stepCompleted = true;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var runTask = runner.RunAsync(CancellationToken.None);

        var awaitTask = Task.Run(async () => await runner, TestContext.Current.CancellationToken);
        var awaitTaskConfigureAwaitT = Task.Run(async () => await runner.ConfigureAwait(true), TestContext.Current.CancellationToken);
        var awaitTaskConfigureAwaitF = Task.Run(async () => await runner.ConfigureAwait(false), TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaitTask.IsCompleted, "Awaiter should block while runner is executing");
        Assert.False(awaitTaskConfigureAwaitT.IsCompleted, "Awaiter should block while runner is executing");
        Assert.False(awaitTaskConfigureAwaitF.IsCompleted, "Awaiter should block while runner is executing");
        Assert.False(stepCompleted);

        tcs.SetResult(true);

        await awaitTask;
        await awaitTaskConfigureAwaitT;
        await awaitTaskConfigureAwaitF;

        Assert.True(awaitTask.IsCompleted);
        Assert.True(awaitTaskConfigureAwaitT.IsCompleted);
        Assert.True(awaitTaskConfigureAwaitF.IsCompleted);
        
        Assert.True(stepCompleted);
        
        await runTask;
    }

    [Fact]
    public async Task GetAwaiter_AfterRun_CompletesImmediately()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        var awaiter = runner.GetAwaiter();
        var awaitConfigureAwaitT = runner.ConfigureAwait(true);
        var awaitConfigureAwaitF = runner.ConfigureAwait(false);

        Assert.True(awaiter.IsCompleted);
        Assert.True(awaitConfigureAwaitT.GetAwaiter().IsCompleted);
        Assert.True(awaitConfigureAwaitF.GetAwaiter().IsCompleted);

        await runner;
        await runner.ConfigureAwait(false);
        await runner.ConfigureAwait(true);
    }

    [Fact]
    public async Task GetAwaiter_MultipleAwaitersBeforeRun_AllCompleteWhenRunFinishes()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(async _ => await Task.Delay(50, TestContext.Current.CancellationToken), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var awaitTask1 = Task.Run(async () => await runner, TestContext.Current.CancellationToken);
        var awaitTask2 = Task.Run(async () => await runner.ConfigureAwait(true), TestContext.Current.CancellationToken);
        var awaitTask3 = Task.Run(async () => await runner.ConfigureAwait(false), TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaitTask1.IsCompleted);
        Assert.False(awaitTask2.IsCompleted);
        Assert.False(awaitTask3.IsCompleted);

        await runner.RunAsync(CancellationToken.None);

        await awaitTask1;
        await awaitTask2;
        await awaitTask3;
        
        Assert.True(awaitTask1.IsCompleted);
        Assert.True(awaitTask2.IsCompleted);
        Assert.True(awaitTask3.IsCompleted);
    }

    [Fact]
    public async Task GetAwaiter_WithStepErrors_CompletesWithoutThrowing()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => throw new InvalidOperationException("Test error"), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var runTask = runner.RunAsync(CancellationToken.None);

        var eList = new List<Exception?>
        {
            await Record.ExceptionAsync(async () => await runner),
            await Record.ExceptionAsync(async () => await runner.ConfigureAwait(false)),
            await Record.ExceptionAsync(async () => await runner.ConfigureAwait(true))
        };

        Assert.All(eList, Assert.Null);
        Assert.NotNull(runner.Exception);

        await runTask;
    }

    [Fact]
    public async Task GetAwaiter_CalledMultipleTimesBeforeAndAfterRun_AllSucceed()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        Assert.False(runner.GetAwaiter().IsCompleted);
        Assert.False(runner.ConfigureAwait(false).GetAwaiter().IsCompleted);
        Assert.False(runner.ConfigureAwait(true).GetAwaiter().IsCompleted);

        await runner.RunAsync(CancellationToken.None);

        Assert.True(runner.GetAwaiter().IsCompleted);
        Assert.True(runner.ConfigureAwait(false).GetAwaiter().IsCompleted);
        Assert.True(runner.ConfigureAwait(true).GetAwaiter().IsCompleted);
#pragma warning disable xUnit1031
        runner.GetAwaiter().GetResult();
        runner.ConfigureAwait(true).GetAwaiter().GetResult();
        runner.ConfigureAwait(false).GetAwaiter().GetResult();
#pragma warning restore xUnit1031
    }

    [Fact]
    public async Task GetAwaiter_MultipleAwaiters_AllCompleteWhenCancelled()
    {
        var runner = CreateStepRunner();
        var cts = new CancellationTokenSource();
        var stepStarted = new ManualResetEventSlim(false);
        var canCancel = new ManualResetEventSlim(false);

        var step1 = new TestStep(async ct =>
        {
            await Task.Yield();
            stepStarted.Set();
            canCancel.Wait(TestContext.Current.CancellationToken);
            ct.ThrowIfCancellationRequested();
        }, ServiceProvider);

        runner.AddStep(step1);
        FinishAdding(runner);

        // Start multiple awaiters before cancellation
        var awaiterTasks = new[]
        {
            Task.Run(async () => await runner, TestContext.Current.CancellationToken),
            Task.Run(async () => await runner, TestContext.Current.CancellationToken),
            Task.Run(async () => await runner, TestContext.Current.CancellationToken),
        };

        var runTask = runner.RunAsync(cts.Token);

        stepStarted.Wait(TestContext.Current.CancellationToken);

        // Cancel
        cts.Cancel();
        canCancel.Set();

        var allAwaitersTask = Task.WhenAll(awaiterTasks);
        var exception = await Record.ExceptionAsync(() => allAwaitersTask);

        Assert.Null(exception);
    }

    [Fact]
    public void AwaiterAndAwaitableEquality()
    {
        ConfigureAwaitTestExtensions.AwaiterAndAwaitableEquality(
            () =>
            {
                var stepRunner = CreateStepRunner();
                return stepRunner;
            },
            step => step.GetAwaiter(),
            (step, ca) => step.ConfigureAwait(ca));


    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [InlineData(null)]
    public void OnCompleted_CompletesInAnotherSynchronizationContext(bool? continueOnCapturedContext)
    {
        var executeCount = 0;
        var step = new TestStep((_ =>
        {
            Interlocked.Increment(ref executeCount);
            return Task.CompletedTask;
        }), ServiceProvider);

        ConfigureAwaitTestExtensions.TestOnCompletedCompletesInAnotherSynchronizationContext(
            continueOnCapturedContext,
            () => {
                var stepRunner = CreateStepRunner();
                stepRunner.AddStep(step);
                stepRunner.AddStep(step);
                stepRunner.AddStep(step);
                FinishAdding(stepRunner);
                return stepRunner;
            },
            runner => runner.GetAwaiter(),
            (runner, ca) => runner.ConfigureAwait(ca),
            runner => runner.RunAsync(CancellationToken.None));

        Assert.Equal(3, executeCount);
    }

    [Fact]
    public async Task RunAsync_GetAwaiter_ConfigureAwait_ShareSameTask()
    {
        var runner = CreateStepRunner();
        var stepStarted = new TaskCompletionSource<int>();
        var canComplete = new TaskCompletionSource<int>();

        var step = new TestStep(async _ =>
        {
            stepStarted.SetResult(1);
            await canComplete.Task;
        }, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        // Get task from RunAsync
        var runTask = runner.RunAsync(CancellationToken.None);

        await stepStarted.Task;

        // All should report same IsCompleted state while running
        Assert.False(runTask.IsCompleted);
        Assert.False(runner.GetAwaiter().IsCompleted);
        Assert.False(runner.ConfigureAwait(false).GetAwaiter().IsCompleted);
        Assert.False(runner.ConfigureAwait(true).GetAwaiter().IsCompleted);

        canComplete.SetResult(1);
        await runTask;

        // All should now be completed
        Assert.True(runTask.IsCompleted);
        Assert.True(runner.GetAwaiter().IsCompleted);
        Assert.True(runner.ConfigureAwait(false).GetAwaiter().IsCompleted);
        Assert.True(runner.ConfigureAwait(true).GetAwaiter().IsCompleted);
    }

    [Fact]
    public async Task RunAsync_GetAwaiter_ConfigureAwait_PropagateExceptionConsistently()
    {
        var runner = CreateStepRunner();
        var expectedException = new InvalidOperationException("Test");

        var step = new TestStep(_ => throw expectedException, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        var runTask = runner.RunAsync(CancellationToken.None);

        // All should complete without throwing (exceptions are collected in runner.Exception)
        await runTask;
        await runner;
        await runner.ConfigureAwait(false);
        await runner.ConfigureAwait(true);

        // All report same completed state
        Assert.True(runTask.IsCompleted);
        Assert.True(runner.GetAwaiter().IsCompleted);
        Assert.True(runner.ConfigureAwait(false).GetAwaiter().IsCompleted);
        Assert.True(runner.ConfigureAwait(true).GetAwaiter().IsCompleted);

        // Exception is accessible via runner.Exception
        Assert.NotNull(runner.Exception);
        Assert.Contains(expectedException, runner.Exception.InnerExceptions);
    }

    #endregion

    #region ExecutedSteps Tests

    [Fact]
    public void ExecutedSteps_BeforeRun_IsEmpty()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);

        Assert.Empty(runner.ExecutedSteps);
    }

    [Fact]
    public async Task ExecutedSteps_AfterSuccessfulRun_ContainsAllSteps()
    {
        var runner = CreateStepRunner();
        var step1 = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        var step2 = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(2, runner.ExecutedSteps.Count);
        Assert.Contains(step1, runner.ExecutedSteps);
        Assert.Contains(step2, runner.ExecutedSteps);
    }

    [Fact]
    public async Task ExecutedSteps_AfterFailedStep_ContainsFailedStep()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => throw new Exception(), ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Assert.Single(runner.ExecutedSteps);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Assert.Contains(step, runner.ExecutedSteps);
    }

    [Fact]
    public async Task ExecutedSteps_CancelledRun_ContainsOnlyExecutedSteps()
    {
        if (!SupportsSequentialExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: true);
        var cts = new CancellationTokenSource();

        var step1 = new TestStep(_ =>
        {
            cts.Cancel();
            return Task.CompletedTask;
        }, ServiceProvider);
        var step2 = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        await runner.RunAsync(cts.Token);

        Assert.Contains(step1, runner.ExecutedSteps);
        Assert.DoesNotContain(step2, runner.ExecutedSteps);
    }

    [Fact]
    public async Task ExecutedSteps_IsReadOnlyCollection()
    {
        var runner = CreateStepRunner();
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        runner.AddStep(step);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.IsAssignableFrom<IReadOnlyCollection<IStep>>(runner.ExecutedSteps);
    }

    #endregion

    #region WorkerCount Tests

    [Fact]
    public void WorkerCount_IsAtLeastOne()
    {
        var runner = CreateStepRunner();
        Assert.True(runner.WorkerCount >= 1);
    }

    [Fact]
    public void WorkerCount_SequentialRunner_IsOne()
    {
        if (!SupportsSequentialExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: true);
        Assert.Equal(1, runner.WorkerCount);
    }

    [Fact]
    public void WorkerCount_ParallelRunner_IsGreaterThanOne()
    {
        if (HasSequentialStepExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: false);
        Assert.True(runner.WorkerCount > 1);
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task RunAsync_ParallelRunner_AllowsConcurrentExecution()
    {
        if (HasSequentialStepExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: false);
        var workerCount = runner.WorkerCount;

        var barrier = new CountdownEvent(workerCount);
        var allReachedBarrier = false;
        var stepsCompleted = 0;

        for (var i = 0; i < workerCount; i++)
        {
            var step = new TestStep(async _ =>
            {
                await Task.Yield();
                barrier.Signal();
                allReachedBarrier = barrier.Wait(TimeSpan.FromSeconds(5));
                Interlocked.Increment(ref stepsCompleted);
            }, ServiceProvider);
            runner.AddStep(step);
        }

        FinishAdding(runner);
        await runner.RunAsync(CancellationToken.None);

        Assert.True(allReachedBarrier,
            $"All {workerCount} steps should run concurrently, but barrier was not reached");
        Assert.Equal(workerCount, stepsCompleted);
    }

    [Fact]
    public async Task RunAsync_ParallelRunner_RespectsWorkerCount()
    {
        if (HasSequentialStepExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: false);
        var workerCount = runner.WorkerCount;
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        for (var i = 0; i < workerCount * 3; i++)
        {
            var step = new TestStep(async _ =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }

                await Task.Delay(200, TestContext.Current.CancellationToken);

                lock (lockObj)
                {
                    concurrentCount--;
                }
            }, ServiceProvider);
            runner.AddStep(step);
        }

        FinishAdding(runner);
        await runner.RunAsync(CancellationToken.None);

        Assert.True(maxConcurrent <= workerCount,
            $"Max concurrent ({maxConcurrent}) exceeded worker count ({workerCount})");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task AddStep_ConcurrentAdds_AllStepsAdded()
    {
        var runner = CreateStepRunner();
        var executedCount = 0;
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var step = new TestStep(_ =>
                {
                    Interlocked.Increment(ref executedCount);
                    return Task.CompletedTask;
                }, ServiceProvider);
                runner.AddStep(step);
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);
        FinishAdding(runner);
        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(100, executedCount);
    }

    [Fact]
    public async Task Error_ConcurrentErrors_AllErrorsRecorded()
    {
        if (HasSequentialStepExecutionOrder)
            return;
        
        var runner = CreateStepRunner(sequential: false);
        var errorCount = 0;

        runner.Error += (_, _) =>
        {
            Interlocked.Increment(ref errorCount);
        };

        for (var i = 0; i < 50; i++)
        {
            var i1 = i;
            runner.AddStep(new TestStep(_ => throw new Exception($"Error {i1}"), ServiceProvider));
        }

        FinishAdding(runner);
        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(50, errorCount);
        Assert.NotNull(runner.Exception);
        Assert.Equal(50, runner.Exception.InnerExceptions.Count);
    }

    #endregion

    #region Step Dependency / Awaiting Tests

    [Fact]
    public async Task RunAsync_StepAwaitsCompletedStep_CompletesImmediately()
    {
        var runner = CreateStepRunner();
        var step1Completed = false;
        var step2AwaitedSuccessfully = false;
        var step1CompletedEvent = new ManualResetEventSlim(false);

        var step1 = new TestStep(_ =>
        {
            step1Completed = true;
            step1CompletedEvent.Set();
            return Task.CompletedTask;
        }, ServiceProvider);

        var step2 = new TestStep(async _ =>
        {
            step1CompletedEvent.Wait(TestContext.Current.CancellationToken);
            await step1;
            step2AwaitedSuccessfully = step1Completed;
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.True(step1Completed, "Step1 should have completed");
        Assert.True(step2AwaitedSuccessfully, "Step2 should see Step1 as completed");
    }

    [Fact]
    public async Task RunAsync_ParallelRunner_StepAwaitsOtherStep_NoDeadlock()
    {
        if (HasSequentialStepExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: false);

        // Need at least 2 workers
        if (runner.WorkerCount < 2)
            return;

        var step1Started = new ManualResetEventSlim(false);
        var step1CanComplete = new ManualResetEventSlim(false);
        var step1Completed = false;
        var step2CompletedAfterStep1 = false;

        var step1 = new TestStep(_ =>
        {
            step1Started.Set();
            step1CanComplete.Wait(TestContext.Current.CancellationToken);
            step1Completed = true;
            return Task.CompletedTask;
        }, ServiceProvider);

        var step2 = new TestStep(async _ =>
        {
            step1Started.Wait(TestContext.Current.CancellationToken);
            step1CanComplete.Set();
            await step1;
            step2CompletedAfterStep1 = step1Completed;
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        var runTask = runner.RunAsync(CancellationToken.None);
        var completed = await WaitForTaskWithTimeout(runTask, TimeSpan.FromSeconds(10));

        Assert.True(completed, "Runner should complete without deadlock");
        Assert.True(step1Completed, "Step1 should have completed");
        Assert.True(step2CompletedAfterStep1, "Step2 should complete after Step1");
    }

    [Fact]
    public async Task RunAsync_ParallelRunner_MultipleStepsAwaitSameStep_AllComplete()
    {
        if (HasSequentialStepExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: false);

        var awaiterCount = runner.WorkerCount - 1;
        if (awaiterCount < 2)
            return;

        var step1CanComplete = new ManualResetEventSlim(false);
        var awaitersStarted = new CountdownEvent(awaiterCount);
        var completedAwaiters = 0;

        var step1 = new TestStep( _ =>
        {
            awaitersStarted.Wait(TestContext.Current.CancellationToken);
            step1CanComplete.Wait(TestContext.Current.CancellationToken);
            return Task.CompletedTask;
        }, ServiceProvider);

        runner.AddStep(step1);

        for (var i = 0; i < awaiterCount; i++)
        {
            var step = new TestStep(async _ =>
            {
                awaitersStarted.Signal();

                if (awaitersStarted.CurrentCount == 0)
                    step1CanComplete.Set();

                await step1;
                Interlocked.Increment(ref completedAwaiters);
            }, ServiceProvider);
            runner.AddStep(step);
        }

        FinishAdding(runner);

        var runTask = runner.RunAsync(CancellationToken.None);
        var completed = await WaitForTaskWithTimeout(runTask, TimeSpan.FromSeconds(10));

        Assert.True(completed, "Runner should complete without deadlock");
        Assert.Equal(awaiterCount, completedAwaiters);
    }

    [Fact]
    public async Task RunAsync_StepAwaitsFailedStep_ReceivesException()
    {
        var runner = CreateStepRunner();
        var expectedException = new InvalidOperationException("Step1 failed");
        Exception? caughtException = null;

        var step1 = new TestStep(_ => throw expectedException, ServiceProvider);

        var step2 = new TestStep(async _ =>
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);

            try
            {
                await step1;
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(caughtException);
        Assert.Same(expectedException, caughtException);
    }

    [Fact]
    public void RunAsync_SequentialRunner_StepAwaitsLaterStep_WouldDeadlock()
    {
        if (!SupportsSequentialExecutionOrder)
            return;

        var runner = CreateStepRunner(sequential: true);

        var step2 = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        var step1 = new TestStep(async _ =>
        {
            var awaitTask = Task.Run(async () => await step2, TestContext.Current.CancellationToken);
            var completed = await WaitForTaskWithTimeout(awaitTask, TimeSpan.FromMilliseconds(500));

            Assert.False(completed,
                "Awaiting a later step in sequential mode should not complete (would deadlock)");
        }, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);
        FinishAdding(runner);

        var runTask = runner.RunAsync(CancellationToken.None);
#pragma warning disable xUnit1031
        var testCompleted = WaitForTaskWithTimeout(runTask, TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
#pragma warning restore xUnit1031

        Assert.True(testCompleted, "Test should complete (step1 detects the would-be deadlock internally)");
    }

    #endregion
    
    // TODO: Remove
    private static async Task<bool> WaitForTaskWithTimeout(Task task, TimeSpan timeout)
    {
        var delayTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(task, delayTask);
        return completedTask == task;
    }
}