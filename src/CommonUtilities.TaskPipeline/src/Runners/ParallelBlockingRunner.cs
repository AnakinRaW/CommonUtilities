using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sklavenwalker.CommonUtilities.TaskPipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued tasks parallel. Tasks may be queued while task execution has been started.
/// The execution is finished only if <see cref="Finish"/> or <see cref="FinishAndWait"/> was called explicitly.
/// </summary>
public sealed class ParallelBlockingRunner : IParallelRunner
{
    /// <inheritdoc/>
    public event EventHandler<TaskErrorEventArgs>? Error;

    private readonly ConcurrentBag<Exception> _exceptions;
    private readonly ConcurrentBag<ITask> _pipelineTasks;
    private readonly ILogger? _logger;
    private readonly int _workerCount;
    private readonly Task[] _runnerTasks;
    private CancellationToken _cancel;

    private BlockingCollection<ITask> TaskQueue { get; }

    /// <summary>
    /// Aggregates all tasks exceptions, if any happened.
    /// </summary>
    public AggregateException? Exception => _exceptions.Count > 0 ? new AggregateException(_exceptions) : null;

    internal bool IsCancelled { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="ParallelTaskRunner"/> instance.
    /// </summary>
    /// <param name="workerCount">The number of parallel workers.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the number of workers is below 1.</exception>
    public ParallelBlockingRunner(int workerCount, IServiceProvider serviceProvider)
    {
        if (workerCount is < 1 or >= 64)
            throw new ArgumentException("invalid parallel worker count");
        _workerCount = workerCount;
        _runnerTasks = new Task[_workerCount];
        _pipelineTasks = new ConcurrentBag<ITask>();
        TaskQueue = new BlockingCollection<ITask>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _exceptions = new ConcurrentBag<Exception>();
    }

    /// <inheritdoc/>
    public void Run(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _cancel = token;
        for (var index = 0; index < _workerCount; ++index)
            _runnerTasks[index] = Task.Factory.StartNew(RunThreaded, CancellationToken.None);
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
        if (!Task.WaitAll(_runnerTasks, timeout))
            throw new TimeoutException();
    }

    /// <summary>
    /// Signals, this instance does not expect any more tasks.
    /// </summary>
    public void Finish()
    {
        TaskQueue.CompleteAdding();
    }

    /// <summary>
    /// Signals, this instance does not expect any more tasks and waits for finished execution.
    /// </summary>
    /// <param name="throwsException"></param>
    public void FinishAndWait(bool throwsException = false)
    {
        Finish();
        try
        {
            Wait();
        }
        catch
        {
            if (throwsException)
                throw;
        }
    }

    /// <inheritdoc/>
    public void Queue(ITask task)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        TaskQueue.Add(task, CancellationToken.None);
    }

    /// <inheritdoc/>
    public IEnumerator<ITask> GetEnumerator()
    {
        return _pipelineTasks.GetEnumerator();
    }

    private void RunThreaded()
    {
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancel);
        var canceled = false;
        foreach (var task in TaskQueue.GetConsumingEnumerable())
        {
            try
            {
                _cancel.ThrowIfCancellationRequested();
                _pipelineTasks.Add(task);
                task.Run(_cancel);
            }
            catch (StopTaskRunnerException)
            {
                _logger?.LogTrace("Stop subsequent tasks");
                TaskQueue.CompleteAdding();
                break;
            }
            catch (Exception ex)
            {
                _exceptions.Add(ex);
                if (!canceled)
                {
                    if (ex.IsExceptionType<OperationCanceledException>())
                        _logger?.LogTrace($"Activity threw exception {ex.GetType()}: {ex.Message}" + Environment.NewLine + $"{ex.StackTrace}");
                    else
                        _logger?.LogTrace(ex, $"Activity threw exception {ex.GetType()}: {ex.Message}");
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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void OnError(TaskErrorEventArgs e)
    {
        Error?.Invoke(this, e);
        if (!e.Cancel)
            return;
        IsCancelled |= e.Cancel;
    }
}