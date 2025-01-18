using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// The execution engine to run one or many <see cref="IStep"/>s.
/// </summary>
public interface IStepRunner
{
    /// <summary>
    /// The event that is raised when the execution of an <see cref="IStep"/> fails with an exception.
    /// </summary>
    event EventHandler<StepErrorEventArgs>? Error;

    /// <summary>
    /// Gets a read-only list of only those steps were executed by the <see cref="IStepRunner"/>.
    /// </summary>
    public IReadOnlyCollection<IStep> ExecutedSteps { get; }

    /// <summary>
    /// Runs all queued steps.
    /// </summary>
    /// <param name="token">The cancellation token, allowing the stepRunner to cancel the operation.</param>
    /// <returns>A task that represents the completion of the operation.</returns>
    Task RunAsync(CancellationToken token);

    /// <summary>
    /// Adds an <see cref="IStep"/> to the <see cref="IStepRunner"/>.
    /// </summary>
    /// <param name="step">The step to app.</param>
    /// /// <exception cref="ArgumentNullException"><paramref name="step"/> is <see langword="null"/>.</exception>
    void AddStep(IStep step);
}