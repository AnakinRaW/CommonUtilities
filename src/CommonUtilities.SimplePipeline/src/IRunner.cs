using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Execution engine to run one or many <see cref="IStep"/>s.
/// </summary>
public interface IRunner : IStepQueue, IDisposable
{
    /// <summary>
    /// Gets raised when the execution of an <see cref="IStep"/> failed with an exception.
    /// </summary>
    event EventHandler<StepErrorEventArgs>? Error;

    /// <summary>
    /// Runs all queued steps.
    /// </summary>
    /// <param name="token">The cancellation token, allowing the runner to cancel the operation.</param>
    /// <returns>A task that represents the completion of the operation.</returns>
    Task RunAsync(CancellationToken token);
}