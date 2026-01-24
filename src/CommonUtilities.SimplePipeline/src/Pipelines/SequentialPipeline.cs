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

    /// <summary>
    /// Creates an instance of <see cref="IStepRunner"/> to execute the steps in the pipeline sequentially.
    /// </summary>
    /// <remarks>
    /// This method returns a <see cref="SequentialStepRunner"/>.
    /// The runner ensures that all steps are executed one after another in a sequential manner.
    /// </remarks>
    /// <returns>
    /// An instance of <see cref="IStepRunner"/> that executes steps sequentially.
    /// </returns>
    protected sealed override IStepRunner CreateRunner()
    {
        return new SequentialStepRunner(ServiceProvider);
    }
}