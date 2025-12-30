using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

/// <summary>
/// Abstract base class for testing PipelineStep implementations.
/// Subclasses MUST define expected behavior via abstract properties.
/// </summary>
public abstract class PipelineStepTestBase : TestBaseWithServiceProvider
{
    /// <summary>
    /// Factory method to create a step for basic testing.
    /// </summary>
    protected abstract PipelineStep CreateStep();

    /// <summary>
    /// Factory method to create a step that performs an async action.
    /// </summary>
    protected abstract PipelineStep CreateStepWithAction(Func<CancellationToken, Task> action);

    /// <summary>
    /// Defines whether this step respects cancellation tokens.
    /// </summary>
    protected abstract bool StepRespectsCancellationToken { get; }

    /// <summary>
    /// Defines whether exceptions thrown in RunCoreAsync are added to the Error property.
    /// </summary>
    protected abstract bool StepAddsExceptionsToErrorProperty { get; }

    /// <summary>
    /// Defines whether StopRunnerException is added to the Error property.
    /// </summary>
    protected abstract bool StepAddsStopRunnerExceptionToErrorProperty { get; }

    /// <summary>
    /// Defines whether the Step throws exceptions at all
    /// </summary>
    protected virtual bool StepThrowsExceptions => true;

    /// <summary>
    /// Defines the exception type that is propagated when the step throws an exception.
    /// Return null if the step does not throw exceptions
    /// </summary>
    protected abstract Type? GetExpectedExceptionType(Exception thrownException);

    #region Dispose

    [Fact]
    public void Disposed()
    {
        var step = CreateStep();

        step.Dispose();
        Assert.True(step.IsDisposed);
    }

    #endregion

    #region RunAsync

    [Fact]
    public async Task RunAsync_TaskAction()
    {
        var ran = false;
        var step = CreateStepWithAction(_ =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        await step.RunAsync(CancellationToken.None);

        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_AwaitedAction()
    {
        var ran = false;
        var step = CreateStepWithAction(async _ =>
        {
            await Task.Yield();
            ran = true;
        });

        await step.RunAsync(CancellationToken.None);

        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_ThrowsException()
    {
        var expectedError = new InvalidOperationException("Test");
        var step = CreateStepWithAction(_ => throw expectedError);

        var expectedType = GetExpectedExceptionType(expectedError);

        if (expectedType != null)
        {
            await Assert.ThrowsAsync(expectedType, () => step.RunAsync(CancellationToken.None));

            if (StepAddsExceptionsToErrorProperty)
            {
                Assert.NotNull(step.Error);
                // For transformed exceptions, check if original is preserved somehow
                if (expectedType == expectedError.GetType())
                    Assert.Same(expectedError, step.Error);
            }
            else
            {
                Assert.Null(step.Error);
            }
        }
        else
        {
            await step.RunAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task RunAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        if (!StepRespectsCancellationToken)
        {
            // Skip test or verify step ignores cancellation
            var tcs = new TaskCompletionSource<bool>();
            var step = CreateStepWithAction(async _ => await tcs.Task);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var runTask = step.RunAsync(cts.Token);
            await Task.Delay(50, TestContext.Current.CancellationToken);

            Assert.False(runTask.IsCompleted); // Step ignores cancellation
            tcs.SetResult(true);
            await runTask;
        }
        else
        {
            var step2 = CreateStepWithAction(ct =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

            var cts2 = new CancellationTokenSource();
            cts2.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => step2.RunAsync(cts2.Token));
            Assert.Null(step2.Error);
        }
    }

    [Fact]
    public async Task RunAsync_StopRunnerException_IsNotAddedToErrors()
    {
        if (!StepThrowsExceptions)
            return;

        var step = CreateStepWithAction(_ => throw new StopRunnerException());

        var expectedType = GetExpectedExceptionType(new StopRunnerException())!;
        await Assert.ThrowsAsync(expectedType, () => step.RunAsync(CancellationToken.None));

        if (StepAddsStopRunnerExceptionToErrorProperty)
            Assert.NotNull(step.Error);
        else
            Assert.Null(step.Error);
    }

    [Fact]
    public async Task RunAsync_AggregateException()
    {
        if (!StepThrowsExceptions)
            return;

        var innerException = new Exception("Test");
        var expected = new AggregateException(innerException);
        var step = CreateStepWithAction(_ => throw expected);

        var expectedType = GetExpectedExceptionType(expected)!;
        await Assert.ThrowsAsync(expectedType, () => step.RunAsync(CancellationToken.None));

        if (StepAddsExceptionsToErrorProperty)
        {
            Assert.NotNull(step.Error);
            if (expectedType == typeof(AggregateException))
                Assert.Same(expected, step.Error);
        }
        else
        {
            Assert.Null(step.Error);
        }
    }

    [Fact]
    public async Task RunAsync_AggregateException_OriginatedFromOperationCancelled()
    {
        if (!StepThrowsExceptions)
            return;
        
        var innerException = new Exception("Test");
        var expected = new AggregateException(new OperationCanceledException(null, innerException));
        var step = CreateStepWithAction(_ => throw expected);

        var expectedType = GetExpectedExceptionType(expected)!;
        await Assert.ThrowsAsync(expectedType, () => step.RunAsync(CancellationToken.None));

        if (StepAddsExceptionsToErrorProperty)
        {
            Assert.NotNull(step.Error);
            // Should unwrap to inner exception
            Assert.Same(innerException, step.Error);
        }
        else
        {
            Assert.Null(step.Error);
        }
    }

    [Fact]
    public async Task RunAsync_AggregateException_OriginatedFromOperationCancelled_NoInnerException()
    {
        if (!StepThrowsExceptions)
            return;
        
        var expected = new AggregateException(new OperationCanceledException());
        var step = CreateStepWithAction(_ => throw expected);

        var expectedType = GetExpectedExceptionType(expected)!;
        await Assert.ThrowsAsync(expectedType, async () => await step.RunAsync(CancellationToken.None));
        Assert.Null(step.Error);
    }

    #endregion

    #region GetAwaiter

    [Fact]
    public async Task GetAwaiter_AfterCompletion_ReturnsImmediately()
    {
        var executed = false;
        var step = CreateStepWithAction(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        await step.RunAsync(CancellationToken.None);

        await step;
        Assert.True(executed);
    }

    [Fact]
    public async Task GetAwaiter_BeforeStart_WaitsForCompletion()
    {
        var tcs = new TaskCompletionSource<bool>();
        var step = CreateStepWithAction(async _ =>
        {
            await tcs.Task;
        });

        var awaiterTask = Task.Run(async () => await step, TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaiterTask.IsCompleted);

        var runTask = step.RunAsync(CancellationToken.None);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaiterTask.IsCompleted);

        tcs.SetResult(true);
        await runTask;

        await awaiterTask;
        Assert.True(awaiterTask.IsCompleted);
    }

    [Fact]
    public async Task GetAwaiter_DuringExecution_WaitsForCompletion()
    {
        var tcs = new TaskCompletionSource<bool>();
        var step = CreateStepWithAction(async _ =>
        {
            await tcs.Task;
        });

        var runTask = step.RunAsync(CancellationToken.None);

        var awaiterTask = Task.Run(async () => await step, TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaiterTask.IsCompleted);

        tcs.SetResult(true);
        await runTask;

        await awaiterTask;
        Assert.True(awaiterTask.IsCompleted);
    }

    [Fact]
    public async Task GetAwaiter_MultipleAwaiters_AllComplete()
    {
        var tcs = new TaskCompletionSource<bool>();
        var step = CreateStepWithAction(async _ =>
        {
            await tcs.Task;
        });

        var awaiter1 = Task.Run(async () => await step, TestContext.Current.CancellationToken);
        var awaiter2 = Task.Run(async () => await step, TestContext.Current.CancellationToken);
        var awaiter3 = Task.Run(async () => await step, TestContext.Current.CancellationToken);

        var runTask = step.RunAsync(CancellationToken.None);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(awaiter1.IsCompleted);
        Assert.False(awaiter2.IsCompleted);
        Assert.False(awaiter3.IsCompleted);

        tcs.SetResult(true);
        await runTask;

        await Task.WhenAll(awaiter1, awaiter2, awaiter3);
        Assert.True(awaiter1.IsCompleted);
        Assert.True(awaiter2.IsCompleted);
        Assert.True(awaiter3.IsCompleted);
    }

    [Fact]
    public async Task GetAwaiter_AfterSuccessfulRun_CanBeAwaitedMultipleTimes()
    {
        var executionCount = 0;
        var step = CreateStepWithAction(_ =>
        {
            Interlocked.Increment(ref executionCount);
            return Task.CompletedTask;
        });

        await step.RunAsync(CancellationToken.None);
        Assert.Equal(1, executionCount);

        await step;
        await step;
        await step;

        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task GetAwaiter_PropagatesException()
    {
        if (!StepThrowsExceptions)
            return;
        
        var expected = new InvalidOperationException("Test error");
        var step = CreateStepWithAction(_ => throw expected);

        var expectedType = GetExpectedExceptionType(expected)!;

        await Assert.ThrowsAsync(expectedType, () => step.RunAsync(CancellationToken.None));
        await Assert.ThrowsAsync(expectedType, async () => await step);
    }

    [Fact]
    public async Task GetAwaiter_WithCancellation_PropagatesCancellation()
    {
        if (!StepRespectsCancellationToken || !StepThrowsExceptions)
            return;

        var step = CreateStepWithAction(ct =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => step.RunAsync(cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await step);
    }

    #endregion
}