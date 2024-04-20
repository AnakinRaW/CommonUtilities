using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Execution engine to run one or many <see cref="IStep"/>s.
/// </summary>
public interface IRunner : IDisposable
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

    /// <summary>
    /// List of only those steps which are scheduled for execution of an <see cref="IRunner"/>.
    /// </summary>
    public IReadOnlyList<IStep> Steps { get; }

    /// <summary>
    /// Adds an <see cref="IStep"/> to the <see cref="IRunner"/>.
    /// </summary>
    /// <param name="activity">The step to app.</param>
    void AddStep(IStep activity);
}