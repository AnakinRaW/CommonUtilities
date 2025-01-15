using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued steps parallel.
/// </summary>
public class ParallelStepRunner: StepRunner, ISynchronizedStepRunner
{
    private readonly ConcurrentBag<Exception> _exceptions;
    private readonly Task[] _tasks;
    private CancellationToken _cancel;

    /// <summary>
    /// Gets the number of parallel workers.
    /// </summary>
    public int WorkerCount { get; }

    /// <summary>
    /// Gets an aggregated exception of all failed steps.
    /// </summary>
    public AggregateException? Exception => _exceptions.Count > 0 ? new AggregateException(_exceptions) : null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelStepRunner"/> class with the specified number of workers.
    /// </summary>
    /// <param name="workerCount">The number of parallel workers.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the number of workers is below 1.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public ParallelStepRunner(int workerCount, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        if (workerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(workerCount));
        WorkerCount = workerCount;
        _exceptions = [];
        _tasks = new Task[workerCount];
    }

    /// <inheritdoc/>
    public override Task RunAsync(CancellationToken token)
    {
        Invoke(token);
        return Task.WhenAll(_tasks);
    }

    /// <inheritdoc/>
    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc/>
    public void Wait(TimeSpan timeout)
    {
        if (!Task.WaitAll(_tasks, timeout))
            throw new TimeoutException();

        var exception = Exception;
        if (exception != null)
            throw exception;
    }

    /// <summary>
    /// Runs all scheduled steps on the thread-pool. 
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected override void Invoke(CancellationToken token)
    {
        ThrowIfCancelled(token);
        StepList.AddRange(StepQueue);
        _cancel = token;

        for (var index = 0; index < WorkerCount; ++index) 
            _tasks[index] = Task.Factory.StartNew(InvokeThreaded, TaskCreationOptions.LongRunning);
    }

    private void InvokeThreaded()
    {
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancel);
        var canceled = false;
        while (StepQueue.TryDequeue(out var step))
        {
            ThrowIfDisposed();
            try
            {
                ThrowIfCancelled(_cancel);
                step.Run(_cancel);
            }
            catch (Exception ex)
            {
                _exceptions.Add(ex);
                if (!canceled)
                {
                    if (ex.IsExceptionType<OperationCanceledException>())
                        Logger?.LogTrace($"Step threw exception {ex.GetType()}: {ex.Message}" + Environment.NewLine + $"{ex.StackTrace}");
                    else
                        Logger?.LogTrace(ex, $"Step threw exception {ex.GetType()}: {ex.Message}");
                }
                var e = new StepErrorEventArgs(step)
                {
                    Cancel = _cancel.IsCancellationRequested || IsCancelled || ex.IsExceptionType<OperationCanceledException>()
                };
                OnError(e);
                if (e.Cancel)
                {
                    canceled = true;
                    linkedTokenSource.Cancel();
                }
            }
        }
    }
}