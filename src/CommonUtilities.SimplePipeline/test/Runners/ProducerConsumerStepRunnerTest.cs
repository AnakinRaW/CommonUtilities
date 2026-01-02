using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class ProducerConsumerStepRunnerTest : StepRunnerTestBase<ProducerConsumerStepRunner>
{
    public override bool HasSequentialStepExecutionOrder => false;

    public override bool SupportsSequentialExecutionOrder => true;

    protected override bool SupportsAddingStepsAfterCancellation => false;

    protected override ProducerConsumerStepRunner CreateStepRunner(bool? sequential = null)
    {
        var workers = sequential is true ? 1 : 4;
        return CreateStepRunner(workers);
    }

    protected override ProducerConsumerStepRunner CreateStepRunner(int workerCount)
    {
        return new ProducerConsumerStepRunner(workerCount, ServiceProvider);
    }

    protected override void FinishAdding(ProducerConsumerStepRunner runner)
    {
        base.FinishAdding(runner);
        runner.Finish();
    }

    #region Finish

    [Fact]
    public void Finish_CalledMultipleTimes()
    {
        var runner = CreateStepRunner();
        runner.Finish();
        runner.Finish();
        runner.Finish();
    }

    [Fact]
    public async Task Finish_CalledBeforeRun_AllStepsExecute()
    {
        var runner = CreateStepRunner();
        var executedSteps = new ConcurrentBag<int>();

        for (var i = 0; i < 10; i++)
        {
            var index = i;
            runner.AddStep(new TestStep(_ =>
            {
                executedSteps.Add(index);
                return Task.CompletedTask;
            }, ServiceProvider));
        }

        runner.Finish();
        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(10, executedSteps.Count);
    }

    [Fact]
    public void Finish_AddStepAfterFinish_ThrowsException()
    {
        var runner = CreateStepRunner();
        runner.Finish();

        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        Assert.Throws<InvalidOperationException>(() => runner.AddStep(step));
    }

    [Fact]
    public async Task Finish_WhileRunning_AllQueuedStepsComplete()
    {
        var runner = CreateStepRunner(workerCount: 2);
        var executedSteps = new ConcurrentBag<int>();
        var step1Started = new ManualResetEventSlim(false);
        var allStepsAdded = new ManualResetEventSlim(false);

        runner.AddStep(new TestStep(async _ =>
        {
            await Task.Yield();
            step1Started.Set();
            executedSteps.Add(1);
            allStepsAdded.Wait(TestContext.Current.CancellationToken);
        }, ServiceProvider));

        var runTask = runner.RunAsync(CancellationToken.None);

        step1Started.Wait(TestContext.Current.CancellationToken);

        for (var i = 2; i <= 5; i++)
        {
            var index = i;
            runner.AddStep(new TestStep(_ =>
            {
                executedSteps.Add(index);
                return Task.CompletedTask;
            }, ServiceProvider));
        }
        allStepsAdded.Set();

        runner.Finish();
        await runTask;

        Assert.Equal(5, executedSteps.Count);
    }

    #endregion

    #region As Sequential

    [Fact]
    public async Task Sequential_TakeNextStep_BlocksUntilStepAvailable()
    {
        var runner = CreateStepRunner(workerCount: 1);
        var step1Started = new ManualResetEventSlim(false);
        var step2Added = new ManualResetEventSlim(false);
        var executedSteps = new ConcurrentBag<int>();

        runner.AddStep(new TestStep(async _ =>
        {
            await Task.Yield();
            step1Started.Set();
            executedSteps.Add(1);
            step2Added.Wait(TestContext.Current.CancellationToken);
        }, ServiceProvider));

        var runTask = runner.RunAsync(CancellationToken.None);

        step1Started.Wait(TestContext.Current.CancellationToken);

        runner.AddStep(new TestStep(_ =>
        {
            executedSteps.Add(2);
            return Task.CompletedTask;
        }, ServiceProvider));

        step2Added.Set();
        runner.Finish();
        await runTask;

        Assert.Equal(2, executedSteps.Count);
        Assert.Contains(1, executedSteps);
        Assert.Contains(2, executedSteps);
    }

    [Fact]
    public async Task Error_SetCancelToTrue_Sequential_StepsInQueue_AreNotExecuted()
    {
        var runner = CreateStepRunner(workerCount: 1);
        var executedSteps = new ConcurrentBag<int>();
        var errorOccurred = new ManualResetEventSlim(false);
        var barrier = new ManualResetEventSlim(false);

        runner.Error += (_, args) =>
        {
            args.Cancel = true;
            errorOccurred.Set();
        };

        runner.AddStep(new TestStep(async _ =>
        {
            await Task.Yield();
            executedSteps.Add(1);
            barrier.Wait(TestContext.Current.CancellationToken);
            throw new InvalidOperationException("Test error");
        }, ServiceProvider));

        for (var i = 2; i <= 5; i++)
        {
            var index = i;
            runner.AddStep(new TestStep(_ =>
            {
                executedSteps.Add(index);
                return Task.CompletedTask;
            }, ServiceProvider));
        }

        runner.Finish();
        var runTask = runner.RunAsync(CancellationToken.None);

        await Task.Delay(100, TestContext.Current.CancellationToken);
        barrier.Set();

        errorOccurred.Wait(TestContext.Current.CancellationToken);
        await runTask;

        Assert.Contains(1, executedSteps);
        Assert.DoesNotContain(2, executedSteps);
        Assert.DoesNotContain(3, executedSteps);
        Assert.DoesNotContain(4, executedSteps);
        Assert.DoesNotContain(5, executedSteps);
    }

    #endregion

    #region RunAsync Extended Behavior

    [Fact]
    public async Task RunAsync_MultipleWorkers_ExecutesStepsConcurrently()
    {
        const int workerCount = 4;
        const int totalSteps = 20;
        var runner = CreateStepRunner(workerCount);
        var executedSteps = new ConcurrentBag<int>();

        var concurrentCount = 0;
        var maxConcurrentCount = 0;
        var lockObj = new object();

        for (var i = 0; i < totalSteps; i++)
        {
            var index = i;
            runner.AddStep(new TestStep(async _ =>
            {
                var current = Interlocked.Increment(ref concurrentCount);
                lock (lockObj)
                {
                    if (current > maxConcurrentCount)
                        maxConcurrentCount = current;
                }

                await Task.Delay(new Random().Next(50, 300), TestContext.Current.CancellationToken);
                executedSteps.Add(index);

                Interlocked.Decrement(ref concurrentCount);
            }, ServiceProvider));
        }

        runner.Finish();
        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(totalSteps, executedSteps.Count);
        Assert.True(maxConcurrentCount >= 2, $"Expected concurrent execution, but max concurrent was {maxConcurrentCount}");
    }

    [Fact]
    public async Task RunAsync_NotFinished_NeverEnds()
    {
        var runner = CreateStepRunner();

        var tsc1 = new TaskCompletionSource<int>();
        var tsc2 = new TaskCompletionSource<int>();

        var s1 = new TestStep(_ => { tsc1.SetResult(1); return Task.CompletedTask; }, ServiceProvider);
        var s2 = new TestStep(_ => { tsc2.SetResult(1); return Task.CompletedTask; }, ServiceProvider);

        runner.AddStep(s1);
        runner.AddStep(s2);

        _ = runner.RunAsync(CancellationToken.None);

        await tsc1.Task;
        await tsc2.Task;

        Assert.Throws<TimeoutException>(() => runner.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task RunAsync_AddStep_AfterCancellation()
    {
        var runner = CreateStepRunner();

        StepRunnerErrorEventArgs? raisedArgs = null;
        runner.Error += (_, args) =>
        {
            raisedArgs = args;
            Assert.Null(args.Step);
            Assert.True(args.Cancel);
        };

        var tcs = new TaskCompletionSource<int>();

        var ran1 = false;
        var s1 = new TestStep(async _ =>
        {
            await Task.Yield();
            ran1 = true;
            tcs.SetResult(0);
        }, ServiceProvider);

        runner.AddStep(s1);

        var cts = new CancellationTokenSource();

        var runTask = runner.RunAsync(cts.Token);

        Task.Run(async () =>
        {
            await tcs.Task.ConfigureAwait(false);

            // Give it some time, so ensure the runner is internally blocking and waiting for the next step.
            await Task.Delay(1000, CancellationToken.None);
            cts.Cancel();
            Assert.Throws<InvalidOperationException>(() => runner.AddStep(new TestStep(_ => Task.CompletedTask, ServiceProvider)));
        }, CancellationToken.None).Forget();


        await runTask;

        Assert.True(ran1);
        Assert.Equal([s1], runner.ExecutedSteps);

        Assert.True(runner.IsCancelled);
        Assert.Null(runner.Exception);
        Assert.NotNull(raisedArgs);
    }

    #endregion

    #region Automatic Finish

    [Fact]
    public async Task Finished_OnStopRunnerException_RunnerFinishes()
    {
        var runner = CreateStepRunner(workerCount: 4);

        StepRunnerErrorEventArgs? raisedArgs = null;
        runner.Error += (_, args) =>
        {
            Assert.True(args.Cancel);
            raisedArgs = args;
        };

        runner.AddStep(new TestStep(_ => throw new StopRunnerException(), ServiceProvider));

        await runner.RunAsync(CancellationToken.None);

        Assert.NotNull(raisedArgs);

        var step2 = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        Assert.Throws<InvalidOperationException>(() => runner.AddStep(step2));
    }

    [Fact]
    public async Task Finished_CancellationShouldFinish()
    {
        var runner = CreateStepRunner();

        StepRunnerErrorEventArgs? raisedArgs = null;
        runner.Error += (_, args) =>
        {
            raisedArgs = args;
            Assert.Null(args.Step);
            Assert.True(args.Cancel);
        };

        var cts = new CancellationTokenSource();
        var task = runner.RunAsync(cts.Token);

        // Give it some time
        await Task.Delay(500, CancellationToken.None);

        cts.Cancel();

        await task;

        Assert.Throws<InvalidOperationException>(() => runner.AddStep(new TestStep(_ => Task.CompletedTask, ServiceProvider)));

        Assert.True(runner.IsCancelled);
        Assert.Null(runner.Exception);
        Assert.NotNull(raisedArgs);
    }

    #endregion
}