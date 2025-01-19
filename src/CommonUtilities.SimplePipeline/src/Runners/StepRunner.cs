using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued <see cref="IStep"/> sequentially in the order they are queued. 
/// </summary>
public sealed class SequentialStepRunner : StepRunnerBase
{
    private ConcurrentQueue<IStep> StepQueue { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialStepRunner"/> class.
    /// </summary>
    /// <param name="services">The service provider for this instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public SequentialStepRunner(IServiceProvider services) : base(services)
    {
    }

    /// <inheritdoc/>
    public override Task RunAsync(CancellationToken token)
    {
        return Task.Run(() =>
        {
            RunSteps(token);
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    public override void AddStep(IStep step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));
        StepQueue.Enqueue(step);
    }

    /// <inheritdoc />
    protected override bool TakeNextStep(CancellationToken cancellationToken, [NotNullWhen(true)] out IStep? step)
    {
        return StepQueue.TryDequeue(out step);
    }
}