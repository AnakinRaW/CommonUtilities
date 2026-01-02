using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Represents an asynchronous step runner that manages the execution of steps in a pipeline.
/// </summary>
/// <remarks>
/// This class provides functionality to add steps, execute them asynchronously, and handle errors during execution.
/// It supports multiple workers for parallel step execution and ensures proper cancellation and error handling.
/// </remarks>
public class AsyncStepRunner : IStepRunner
{
    /// <inheritdoc />
    public event EventHandler<StepRunnerErrorEventArgs>? Error;

    private readonly ConcurrentQueue<IStep> _pendingSteps = new();
    private readonly ConcurrentBag<IStep> _executedSteps = [];
    private readonly ConcurrentBag<Exception> _exceptions = [];
    private readonly TaskCompletionSource<Task> _completionSource = new();

    /// <inheritdoc />
    public AggregateException? Exception => _exceptions.IsEmpty ? null : new AggregateException(_exceptions);

    /// <summary>
    /// Gets a value indicating whether the steps in the pipeline are executed sequentially.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the steps are executed sequentially; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// The execution is considered sequential when the <see cref="WorkerCount"/> is set to 1.
    /// </remarks>
    public bool IsSequential => WorkerCount == 1;

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <inheritdoc />
    public int WorkerCount { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<IStep> ExecutedSteps => _executedSteps.ToArray();

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Gets the logger instance used for logging messages related to the execution of the step runner.
    /// </summary>
    protected ILogger? Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncStepRunner"/> class with the specified number of workers and a service provider.
    /// </summary>
    /// <param name="workerCount">The number of workers to use for executing steps. Must be between 1 and 64, inclusive.</param>
    /// <param name="serviceProvider">The service provider used to resolve dependencies required by the runner.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="workerCount"/> is less than 1 or greater than 64.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public AsyncStepRunner(int workerCount, IServiceProvider serviceProvider)
    {
        if (workerCount is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(workerCount));
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        WorkerCount = workerCount;
    }

    /// <inheritdoc />
    public virtual void AddStep(IStep step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));
        _pendingSteps.Enqueue(step);
    }

    /// <inheritdoc />
    public Task RunAsync(CancellationToken token)
    {
        if (IsRunning)
            throw new InvalidOperationException("The step runner is already running.");
        
        var task = CreateRunnerTask(token);
        _completionSource.TrySetResult(task);
        return task;
    }

    /// <inheritdoc/>
    public TaskAwaiter GetAwaiter()
    {
        var task = _completionSource.Task;
        return task.IsCompleted 
            ? task.Result.GetAwaiter()
            : GetAwaitableTask().GetAwaiter();
    }

    private async Task GetAwaitableTask()
    {
        var task = await _completionSource.Task.ConfigureAwait(false);
        await task.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc/>
    public void Wait(TimeSpan timeout)
    {
        var tcsTask = _completionSource.Task;
        if (!tcsTask.Wait(timeout))
            throw new TimeoutException();

        var task = tcsTask.Result;

        var completed = true;
        try
        {
            completed = task.Wait(timeout);
        }
        catch
        {
            // Ignore
        }

        if (!completed)
            throw new TimeoutException();

        var exception = Exception;
        if (exception != null)
            throw exception;
    }

    /// <summary>
    /// Attempts to retrieve the next step to be executed from the queue.
    /// </summary>
    /// <param name="step">When this method returns, contains the next <see cref="IStep"/> to be executed if one is available; otherwise, <see langword="null"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a step to become available.</param>
    /// <returns><see langword="true"/> if a step was successfully retrieved; otherwise, <see langword="false"/>.</returns>
    protected virtual bool TakeNextStep([NotNullWhen(true)] out IStep? step, CancellationToken cancellationToken)
    {
        return _pendingSteps.TryDequeue(out step);
    }

    /// <summary>
    /// Allows an overriding class to handle step errors and raises the <see cref="Error"/> event.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="stepError">The event args to use.</param>
    protected virtual void OnError(Exception exception, StepRunnerErrorEventArgs stepError)
    {
        Error?.Invoke(this, stepError);
        IsCancelled |= stepError.Cancel;
    }

    /// <summary>
    /// Throws an <see cref="OperationCanceledException"/> if the given token was requested for cancellation.
    /// </summary>
    /// <param name="token">The token to check for cancellation.</param>
    /// <exception cref="OperationCanceledException">If the token was requested for cancellation.</exception>
    protected void ThrowIfCancelled(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (IsCancelled)
            throw new OperationCanceledException(token);
    }

    /// <summary>
    /// Allows an overriding class to perform cleanup actions once the runner was requested to stop execution.
    /// </summary>
    protected virtual void OnRunnerStopped()
    {
    }

    private async Task CreateRunnerTask(CancellationToken token)
    {
        try
        {
            IsRunning = true;
            if (WorkerCount == 1)
                await Task.Run(() => RunWorkerAsync(token), CancellationToken.None).ConfigureAwait(false);
            else
            {
                var workers = new Task[WorkerCount];
                for (var i = 0; i < WorkerCount; i++)
                    workers[i] = Task.Factory.StartNew(
                        () => RunWorkerAsync(token),
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default).Unwrap(); 
                await Task.WhenAll(workers).ConfigureAwait(false);
            }
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task RunWorkerAsync(CancellationToken token)
    {
        var alreadyCancelled = false;
        try
        {
            while (TakeNextStep(out var step, token))
            {
                try
                {
                    ThrowIfCancelled(token);
                    _executedSteps.Add(step);
                    await step.RunAsync(token).ConfigureAwait(false);
                }
                catch (StopRunnerException e)
                {
                    _exceptions.Add(e);
                    Logger?.LogTrace("Stop subsequent steps");
                    IsCancelled = true;

                    var error = new StepRunnerErrorEventArgs(e, step)
                    {
                        Cancel = true
                    };
                    OnError(e, error);

                    OnRunnerStopped();

                    break;
                }
                catch (Exception e)
                {
                    _exceptions.Add(e);
                    if (!alreadyCancelled)
                    {
                        if (e.IsExceptionType<OperationCanceledException>())
                            Logger?.LogTrace("Step {Step} cancelled", step);
                        else
                            Logger?.LogTrace(e, "Step {Step} threw an exception: {Exception}: {EMessage}", step, e.GetType(), e.Message);
                    }

                    var error = new StepRunnerErrorEventArgs(e, step)
                    {
                        Cancel = token.IsCancellationRequested || IsCancelled || e.IsExceptionType<OperationCanceledException>()
                    };
                    if (error.Cancel)
                        alreadyCancelled = true;
                    OnError(e, error);
                }
            }
        }
        catch (OperationCanceledException e)
        {
            IsCancelled = true;
            OnError(e, new StepRunnerErrorEventArgs(e, null)
            {
                Cancel = true
            });
        }
    }
}