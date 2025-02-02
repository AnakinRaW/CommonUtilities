using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued _steps parallel. Steps may be queued while step execution has been started.
/// The execution can finish only if <see cref="Finish"/> was called explicitly.
/// </summary>
public sealed class ParallelProducerConsumerStepRunner : ParallelStepRunnerBase
{ 
    private BlockingCollection<IStep> StepQueue { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelStepRunner"/> class with the specified number of workers.
    /// </summary>
    /// <param name="workerCount">The number of parallel workers.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the number of workers is below 1.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public ParallelProducerConsumerStepRunner(int workerCount, IServiceProvider serviceProvider) : base(workerCount, serviceProvider)
    {
    }

    /// <summary>
    /// Signals this instance does not expect any more steps.
    /// </summary>
    public void Finish()
    {
        StepQueue.CompleteAdding();
    }

    /// <inheritdoc/>
    public override void AddStep(IStep step)
    {
        if (step is null)
            throw new ArgumentNullException(nameof(step));
        StepQueue.Add(step, CancellationToken.None);
    }

    /// <inheritdoc />
    protected override bool TakeNextStep(CancellationToken cancellationToken, [NotNullWhen(true)] out IStep? step)
    {
        step = null;
        if (StepQueue.IsCompleted)
            return false;

        try
        {
            step = StepQueue.Take(cancellationToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    protected override void OnRunnerStopped()
    {
        Finish();
    }
}