using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Specialized <see cref="IRunner"/> which allows 
/// </summary>
public interface IParallelRunner : IRunner
{
    /// <summary>
    /// Synchronously waits for this runner for all of its tasks to be finished. 
    /// </summary>
    /// <exception cref="AggregateException">If any of the tasks failed with an exception.</exception>
    void Wait();

    /// <summary>
    /// Synchronously waits for this runner for all of its tasks to be finished. 
    /// </summary>
    /// <param name="waitDuration">The time duration to wait.</param>
    /// <exception cref="TimeoutException">If <paramref name="waitDuration"/> expired.</exception>
    void Wait(TimeSpan waitDuration);
}