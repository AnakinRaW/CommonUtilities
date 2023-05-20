﻿using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A simple pipeline that runs all steps sequentially.
/// </summary>
public abstract class SequentialPipeline : SimplePipeline<StepRunner>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection within the pipeline.</param>
    /// <param name="failFast">A value indicating whether the pipeline should fail fast.</param>
    protected SequentialPipeline(IServiceProvider serviceProvider, bool failFast = true) : base(serviceProvider, failFast)
    {
    }

    /// <inheritdoc/>
    protected sealed override StepRunner CreateRunner()
    {
        return new StepRunner(ServiceProvider);
    }
}