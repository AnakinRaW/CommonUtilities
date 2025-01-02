using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A simple pipeline that runs all steps on the thread pool in parallel.
/// </summary>
public abstract class ParallelPipeline : SimplePipeline<ParallelStepRunner>
{
    private readonly int _workerCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection within the pipeline.</param>
    /// <param name="workerCount">The number of worker threads to be used for parallel execution.</param>
    /// <param name="failFast">A value indicating whether the pipeline should fail fast.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected ParallelPipeline(IServiceProvider serviceProvider, int workerCount = 4, bool failFast = true) : base(serviceProvider, failFast)
    {
        _workerCount = workerCount;
    }

    /// <inheritdoc/>
    protected sealed override ParallelStepRunner CreateRunner()
    {
        return new ParallelStepRunner(_workerCount, ServiceProvider);
    }
}