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

internal class ProducerConsumerStepRunner(int workerCount, IServiceProvider serviceProvider)
    : AsyncStepRunner(workerCount, serviceProvider), IDisposable
{
    private readonly BlockingCollection<IStep> _stepQueue = new();
    private bool _disposed;

    protected BlockingCollection<IStep> StepQueue => _stepQueue;

    ~ProducerConsumerStepRunner()
    {
        Dispose(false);
    }

    /// <summary>
    /// Adds a step to the runner. Can be called while the runner is executing.
    /// </summary>
    public override void AddStep(IStep step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);
        _stepQueue.Add(step);
    }

    /// <summary>
    /// Signals this instance does not expect any more steps.
    /// </summary>
    public void Finish()
    {
        if (!_stepQueue.IsAddingCompleted)
            _stepQueue.CompleteAdding();
    }

    protected override bool TakeNextStep([NotNullWhen(true)] out IStep? step, CancellationToken cancellationToken)
    {
        return _stepQueue.TryTake(out step, Timeout.Infinite, cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing) 
            _stepQueue.Dispose();
        _disposed = true;
    }

    protected override void OnRunnerStopped()
    {
        base.OnRunnerStopped();
        Finish();
    }
}

internal class AsyncStepRunner : IStepRunner
{
    public event EventHandler<StepRunnerErrorEventArgs>? Error;

    private readonly ConcurrentQueue<IStep> _pendingSteps = new();
    private readonly ConcurrentBag<IStep> _executedSteps = [];
    private readonly ConcurrentBag<Exception> _exceptions = [];
    
    private Task? _runningTask;

    public AggregateException? Exception => _exceptions.IsEmpty ? null : new AggregateException(_exceptions);
    
    public bool IsRunning { get; private set; }

    public int WorkerCount { get; }

    public IReadOnlyCollection<IStep> ExecutedSteps => _executedSteps.ToArray();

    internal bool IsCancelled { get; private set; }

    protected ILogger? Logger { get; }

    public AsyncStepRunner(int workerCount, IServiceProvider serviceProvider)
    {
        if (workerCount is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(workerCount));
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        WorkerCount = workerCount;
    }

    public virtual void AddStep(IStep step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));
        _pendingSteps.Enqueue(step);
    }

    public Task RunAsync(CancellationToken token)
    {
        if (IsRunning)
            throw new InvalidOperationException("The step runner is already running.");
        
        var task = CreateRunnerTask(token);
        Volatile.Write(ref _runningTask, task);

        return task;
    }

    public void Reset()
    {
        if (IsRunning)
            throw new InvalidOperationException("Cannot reset while step runner is running.");

        while (_exceptions.TryTake(out _)) ;
        while (_executedSteps.TryTake(out _)) ;
        IsCancelled = false;
    }

    public TaskAwaiter GetAwaiter()
    {
        var task = Volatile.Read(ref _runningTask);
        return task?.GetAwaiter() ?? throw new InvalidOperationException("The step runner has not been started.");
    }

    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
    }

    public void Wait(TimeSpan timeout)
    {
        var task = Volatile.Read(ref _runningTask);
        if (task is null)
            throw new InvalidOperationException("The step runner has not been started.");

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
            if (WorkerCount == 1) 
                await RunWorkerAsync(token).ConfigureAwait(false);
            else
            {
                var workers = new Task[WorkerCount];
                for (var i = 0; i < WorkerCount; i++)
                    workers[i] = RunWorkerAsync(token);
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
                catch (StopRunnerException)
                {
                    OnRunnerStopped();
                    Logger?.LogTrace("Stop subsequent steps");
                    IsCancelled = true;
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
            OnError(e, new StepRunnerErrorEventArgs(e, null));
            IsCancelled = true;
        }
    }
}