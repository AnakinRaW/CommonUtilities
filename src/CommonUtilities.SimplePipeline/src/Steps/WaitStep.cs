using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// A step that waits for a given <see cref="ISynchronizedRunner"/> to finish.
/// </summary>
public sealed class WaitStep : PipelineStep
{
    private readonly ISynchronizedRunner _runner;

    /// <summary>
    /// Initializes a new <see cref="WaitStep"/>.
    /// </summary>
    /// <param name="runner">The awaitable step runner</param>
    /// <param name="serviceProvider">The service provider.</param>
    public WaitStep(ISynchronizedRunner runner, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
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