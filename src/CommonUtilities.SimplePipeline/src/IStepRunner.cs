using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// The execution engine to run one or many <see cref="IStep"/>s.
/// </summary>
public interface IStepRunner
{
    /// <summary>
    /// The event that is raised when the execution caused an exception.
    /// </summary>
    event EventHandler<StepRunnerErrorEventArgs>? Error;

    /// <summary>
    /// Gets an aggregated exception of all failed steps or <see langword="null"/> if no step failed.
    /// </summary>
    public AggregateException? Exception { get; }

    /// <summary>
    /// Gets the number of parallel workers the <see cref="IStepRunner"/> uses.
    /// </summary>
    public int WorkerCount { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="IStepRunner"/> is currently executing steps.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the runner is actively executing steps; otherwise, <see langword="false"/>.
    /// </value>
    bool IsRunning { get; }
    
    /// <summary>
    /// Gets a value indicating whether the step runner has been cancelled.
    /// </summary>
    /// <remarks>
    /// This property returns <see langword="true"/> if the step runner was cancelled during its execution,
    /// typically due to a cancellation request via a <see cref="CancellationToken"/>.
    /// Otherwise, it returns <see langword="false"/>.
    /// </remarks>
    bool IsCancelled { get; }

    /// <summary>
    /// Gets a read-only list of only those steps were executed by the <see cref="IStepRunner"/>.
    /// </summary>
    IReadOnlyCollection<IStep> ExecutedSteps { get; }

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

    /// <summary>
    /// Synchronously waits for this stepRunner for all of its steps to be finished. 
    /// </summary>
    /// <exception cref="AggregateException">If any of the steps failed with an exception.</exception>
    void Wait();

    /// <summary>
    /// Synchronously waits for this stepRunner for all of its steps to be finished. 
    /// </summary>
    /// <param name="waitDuration">The time duration to wait.</param>
    /// <exception cref="TimeoutException">If <paramref name="waitDuration"/> expired.</exception>
    /// <exception cref="AggregateException">If any of the steps failed with an exception.</exception>
    void Wait(TimeSpan waitDuration);

    /// <summary>
    /// Gets an awaiter used to await this <see cref="IStepRunner"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method enables the <see cref="IStepRunner"/> to be used with the <c>await</c> keyword.
    /// </para>
    /// <para>
    /// If called before <see cref="RunAsync"/> has been invoked, the awaiter will block until 
    /// the runner is started and has completed execution of all steps.
    /// </para>
    /// <para>
    /// If called during execution, the awaiter will block until all steps have finished.
    /// </para>
    /// <para>
    /// The awaiter does not throw exceptions for failed steps. Any errors that occurred during 
    /// execution are available through the <see cref="Exception"/> property.
    /// </para>
    /// </remarks>
    /// <returns>A <see cref="TaskAwaiter"/> instance that can be used to await the runner's completion.</returns>
    TaskAwaiter GetAwaiter();
}