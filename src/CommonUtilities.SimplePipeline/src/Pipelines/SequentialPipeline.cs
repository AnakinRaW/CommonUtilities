using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A simple pipeline that runs all steps sequentially.
/// </summary>
public abstract class SequentialPipeline : StepRunnerPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection within the pipeline.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected SequentialPipeline(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <inheritdoc/>
    protected sealed override IStepRunner CreateRunner()
    {
        return new SequentialStepRunner(ServiceProvider);
    }
}