using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// A step that waits for a given <see cref="ISynchronizedStepRunner"/> to finish.
/// </summary>
public sealed class WaitStep : PipelineStep
{
    private readonly ISynchronizedStepRunner _stepRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitStep"/> class with the specified stepRunner.
    /// </summary>
    /// <param name="stepRunner">The step runner.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stepRunner"/> or <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public WaitStep(ISynchronizedStepRunner stepRunner, IServiceProvider serviceProvider) : base(serviceProvider)
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
    protected override void RunCore(CancellationToken token)
    {
        try
        {
            _stepRunner.Wait();
        }
        catch
        {
            Logger?.LogTrace("Wait step is stopping all subsequent steps");
            throw new StopRunnerException();
        }
    }
}