using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued steps parallel.
/// </summary>
public class ParallelStepRunner : ParallelStepRunnerBase
{ 
    private ConcurrentQueue<IStep> StepQueue { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelStepRunner"/> class.
    /// </summary>
    /// <param name="workerCount">The number of parallel workers.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the number of workers is below 1 or above 64.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public ParallelStepRunner(int workerCount, IServiceProvider serviceProvider) : base(workerCount, serviceProvider)
    {
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