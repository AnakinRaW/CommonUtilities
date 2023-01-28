using System;
using System.Collections.Generic;
using System.Threading;

namespace AnakinRaW.CommonUtilities.TaskPipeline;

/// <summary>
/// Execution engine to run one or many <see cref="ITask"/>s.
/// </summary>
public interface IRunner : IEnumerable<ITask>
{
    /// <summary>
    /// Event which get's raised if the execution of an <see cref="ITask"/> failed with an exception.
    /// </summary>
    event EventHandler<TaskErrorEventArgs>? Error;

    /// <summary>
    /// Runs all queued tasks
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    void Run(CancellationToken token);

    /// <summary>
    /// Queues an <see cref="ITask"/> for execution.
    /// </summary>
    /// <param name="activity">The task to queue.</param>
    void Queue(ITask activity);
}