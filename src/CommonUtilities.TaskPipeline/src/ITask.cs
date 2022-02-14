using System;
using System.Threading;

namespace Sklavenwalker.CommonUtilities.TaskPipeline;

/// <summary>
/// A task can be queued to an <see cref="IRunner"/> and performs an custom action.
/// </summary>
public interface ITask : IDisposable
{
    /// <summary>
    /// The exception, if any, that happened while running this task.
    /// </summary>
    Exception? Error { get; }

    /// <summary>
    /// Run the task's action.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow task cancellation.</param>
    void Run(CancellationToken token);
}