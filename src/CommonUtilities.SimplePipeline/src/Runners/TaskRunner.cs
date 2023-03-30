using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued <see cref="ITask"/> sequentially in the order they are queued. 
/// </summary>
public class TaskRunner : IRunner
{
    /// <inheritdoc/>
    public event EventHandler<TaskErrorEventArgs>? Error;

    /// <summary>
    /// Modifiable list of all tasks scheduled for execution.
    /// </summary>
    protected readonly List<ITask> TaskList;

    /// <summary>
    /// Queue of all to be performed tasks.
    /// </summary>
    protected ConcurrentQueue<ITask> TaskQueue { get; }

    /// <summary>
    /// The logger instance of this runner.
    /// </summary>
    protected ILogger? Logger { get; }

    /// <summary>
    /// List of all tasks scheduled for execution.
    /// </summary>
    /// <remarks>Tasks queued *after* <see cref="Run"/> was called, are not included.</remarks>
    public IReadOnlyList<ITask> Tasks => new ReadOnlyCollection<ITask>(TaskList);

    internal bool IsCancelled { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="TaskRunner"/> instance.
    /// </summary>
    /// <param name="services"></param>
    public TaskRunner(IServiceProvider services)
    {
        Requires.NotNull(services, nameof(services));
        TaskQueue = new ConcurrentQueue<ITask>();
        TaskList = new List<ITask>();
        Logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <inheritdoc/>
    public void Run(CancellationToken token)
    {
        Invoke(token);
    }

    /// <inheritdoc/>
    public void Queue(ITask activity)
    {
        if (activity == null)
            throw new ArgumentNullException(nameof(activity));
        TaskQueue.Enqueue(activity);
    }

    /// <inheritdoc/>
    public IEnumerator<ITask> GetEnumerator()
    {
        return TaskList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return TaskList.GetEnumerator();
    }

    /// <summary>
    /// Sequentially runs all queued tasks. Faulted tasks will raise the <see cref="Error"/> event.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected virtual void Invoke(CancellationToken token)
    {
        var alreadyCancelled = false;
        TaskList.AddRange(TaskQueue);
        while (TaskQueue.TryDequeue(out var task))
        {
            try
            {
                ThrowIfCancelled(token);
                task.Run(token);
            }
            catch (StopTaskRunnerException)
            {
                Logger?.LogTrace("Stop subsequent tasks");
                break;
            }
            catch (Exception e)
            {
                if (!alreadyCancelled)
                {
                    if (e.IsExceptionType<OperationCanceledException>())
                        Logger?.LogTrace($"Task {task} cancelled");
                    else
                        Logger?.LogTrace(e, $"Task {task} threw an exception: {e.GetType()}: {e.Message}");
                }

                var error = new TaskErrorEventArgs(task)
                {
                    Cancel = token.IsCancellationRequested || IsCancelled ||
                             e.IsExceptionType<OperationCanceledException>()
                };
                if (error.Cancel)
                    alreadyCancelled = true;
                OnError(error);
            }
        }
    }

    /// <summary>
    /// Raises the <see cref="Error"/> event 
    /// </summary>
    /// <param name="e">The event args to use.</param>
    protected virtual void OnError(TaskErrorEventArgs e)
    {
        Error?.Invoke(this, e);
        if (!e.Cancel)
            return;
        IsCancelled |= e.Cancel;
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
}