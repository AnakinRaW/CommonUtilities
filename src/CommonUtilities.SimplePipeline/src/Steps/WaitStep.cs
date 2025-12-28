using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// A step that waits for a given <see cref="IStepRunner"/> to finish.
/// </summary>
public sealed class WaitStep : PipelineStep
{
    private readonly IStepRunner _stepRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitStep"/> class with the specified stepRunner.
    /// </summary>
    /// <param name="stepRunner">The step runner.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stepRunner"/> or <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public WaitStep(IStepRunner stepRunner, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _stepRunner = stepRunner ?? throw new ArgumentNullException(nameof(stepRunner));
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public override string ToString() => "Waiting for other steps";

    /// <summary>
    /// Waits for the instance's parallel stepRunner.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    /// <exception cref="StopRunnerException">If awaiting the stepRunner failed with an exception.</exception>
    protected override async Task RunCoreAsync(CancellationToken token)
    {
        await _stepRunner;
        if (_stepRunner.Exception is not null)
        {
            Logger?.LogTrace("The awaited step runner has exceptions. Stopping all subsequent steps.");
            throw new StopRunnerException();
        }
    }
}