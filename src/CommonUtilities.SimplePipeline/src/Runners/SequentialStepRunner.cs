using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// A <see cref="IStepRunner"/> that executes steps sequentially using a single worker.
/// </summary>
public class SequentialStepRunner(IServiceProvider serviceProvider) : AsyncStepRunner(1, serviceProvider);