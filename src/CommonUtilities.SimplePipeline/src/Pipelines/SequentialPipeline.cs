using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A simple pipeline that runs all steps sequentially.
/// </summary>
public abstract class SequentialPipeline : SimplePipeline<SequentialStepRunner>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection within the pipeline.</param>
    /// <param name="failFast">A value indicating whether the pipeline should fail fast.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected SequentialPipeline(IServiceProvider serviceProvider, bool failFast = true) : base(serviceProvider, failFast)
    {
    }

    /// <inheritdoc/>
    protected sealed override SequentialStepRunner CreateRunner()
    {
        return new SequentialStepRunner(ServiceProvider);
    }
}