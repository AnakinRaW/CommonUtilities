using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A specialized <see cref="IStepRunner"/> which allows for synchronous waiting.
/// </summary>
public interface IParallelStepRunner : IStepRunner
{
    /// <summary>
    /// Gets an aggregated exception of all failed steps or <see langword="null"/> if no step failed.
    /// </summary>
    public AggregateException? Exception { get; }

    /// <summary>
    /// Gets the number of parallel workers the <see cref="IParallelStepRunner"/> uses.
    /// </summary>
    public int WorkerCount { get; }

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
}