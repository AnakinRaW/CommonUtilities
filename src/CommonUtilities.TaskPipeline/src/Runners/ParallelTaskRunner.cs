using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.TaskPipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued tasks parallel.
/// </summary>
public class ParallelTaskRunner: TaskRunner, IParallelRunner
{
    private readonly ConcurrentBag<Exception> _exceptions;
    private readonly Task[] _tasks;
    private CancellationToken _cancel;

    /// <summary>
    /// The number of parallel workers.
    /// </summary>
    public int WorkerCount { get; }

    /// <summary>
    /// Aggregates all tasks exceptions, if any happened.
    /// </summary>
    public AggregateException? Exception => _exceptions.Count > 0 ? new AggregateException(_exceptions) : null;

    /// <summary>
    /// Initializes a new <see cref="ParallelTaskRunner"/> instance.
    /// </summary>
    /// <param name="workerCount">The number of parallel workers.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the number of workers is below 1.</exception>
    public ParallelTaskRunner(int workerCount, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        if (workerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(workerCount));
        WorkerCount = workerCount;
        _exceptions = new ConcurrentBag<Exception>();
        _tasks = new Task[workerCount];
    }

    /// <inheritdoc/>
    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
        var exception = Exception;
        if (exception != null)
            throw exception;
    }

    /// <inheritdoc/>
    public void Wait(TimeSpan timeout)
    {
        if (!Task.WaitAll(_tasks, timeout))
            throw new TimeoutException();
    }

    /// <summary>
    /// Runs all scheduled tasks on the thread-pool. 
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected override void Invoke(CancellationToken token)
    {
        ThrowIfCancelled(token);
        TaskList.AddRange(TaskQueue);
        _cancel = token;
        for (var index = 0; index < WorkerCount; ++index) 
            _tasks[index] = Task.Run(InvokeThreaded, default);
    }

    private void InvokeThreaded()
    {
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancel);
        var canceled = false;
        while (TaskQueue.TryDequeue(out var task))
        {
            try
            {
                ThrowIfCancelled(_cancel);
                task.Run(_cancel);
            }
            catch (Exception ex)
            {
                _exceptions.Add(ex);
                if (!canceled)
                {
                    if (ex.IsExceptionType<OperationCanceledException>())
                        Logger?.LogTrace($"Activity threw exception {ex.GetType()}: {ex.Message}" + Environment.NewLine + $"{ex.StackTrace}");
                    else
                        Logger?.LogTrace(ex, $"Activity threw exception {ex.GetType()}: {ex.Message}");
                }
                var e = new TaskErrorEventArgs(task)
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