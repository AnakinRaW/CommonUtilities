﻿using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A specialized <see cref="IStepRunner"/> which allows for synchronous waiting.
/// </summary>
public interface ISynchronizedStepRunner : IStepRunner
{
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