using System;
using System.Collections.Generic;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Execution engine to run one or many <see cref="IStep"/>s.
/// </summary>
public interface IRunner : IEnumerable<IStep>
{
    /// <summary>
    /// Gets raised when the execution of an <see cref="IStep"/> failed with an exception.
    /// </summary>
    event EventHandler<StepErrorEventArgs>? Error;

    /// <summary>
    /// Runs all queued steps
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    void Run(CancellationToken token);

    /// <summary>
    /// Adds an <see cref="IStep"/> to the <see cref="IRunner"/>.
    /// </summary>
    /// <param name="activity">The step to app.</param>
    void AddStep(IStep activity);
}