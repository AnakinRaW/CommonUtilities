using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// A step that waits for a given <see cref="IParallelRunner"/> to finish.
/// </summary>
public sealed class WaitStep : PipelineStep
{
    private readonly IParallelRunner _runner;

    /// <summary>
    /// Initializes a new <see cref="WaitStep"/>.
    /// </summary>
    /// <param name="runner">The awaitable step runner</param>
    /// <param name="serviceProvider">The service provider.</param>
    public WaitStep(IParallelRunner runner, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Requires.NotNull(runner, nameof(runner));
        _runner = runner;
    }

    /// <inheritdoc/>
    public override string ToString() => "Waiting for other steps";

    /// <summary>
    /// Waits for the instance's parallel runner.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    /// <exception cref="StopRunnerException">If awaiting the runner failed with an exception.</exception>
    protected override void RunCore(CancellationToken token)
    {
        try
        {
            _runner.Wait();
        }
        catch
        {
            Logger?.LogTrace("Wait step is stopping all subsequent steps");
            throw new StopRunnerException();
        }
    }
}