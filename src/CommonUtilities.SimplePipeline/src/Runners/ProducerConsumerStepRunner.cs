using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Represents a step runner that processes steps using a producer-consumer pattern.
/// </summary>
/// <remarks>
/// <para>
/// This class allows steps to be added dynamically while the runner is executing.
/// </para>
/// <para>
/// Call <see cref="Finish"/> to signal completion of step additions,
/// otherwise the runner will block indefinitely unless cancelled via <see cref="CancellationToken"/>.
/// </para>
/// </remarks>
public class ProducerConsumerStepRunner(int workerCount, IServiceProvider serviceProvider)
    : AsyncStepRunner(workerCount, serviceProvider), IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the collection of steps managed by the runner.
    /// </summary>
    /// <remarks>
    /// This property provides access to the underlying <see cref="BlockingCollection{T}"/> 
    /// that stores the steps to be processed.
    /// </remarks>
    protected BlockingCollection<IStep> StepQueue { get; } = new();

    /// <summary>
    /// Finalizes an instance of the <see cref="ProducerConsumerStepRunner"/> class.
    /// </summary>
    ~ProducerConsumerStepRunner()
    {
        Dispose(false);
    }

    /// <summary>
    /// Adds a step to the runner. Can be called while the runner is executing.
    /// </summary>
    public override void AddStep(IStep step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);
        StepQueue.Add(step);
    }

    /// <summary>
    /// Signals this instance does not expect any more steps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must be called to allow <see cref="IStepRunner.RunAsync"/> to complete normally.
    /// After calling this method, attempting to add more steps via <see cref="IStepRunner.AddStep"/>
    /// will throw an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// It is safe to call this method before, during, or after <see cref="IStepRunner.RunAsync"/> execution.
    /// </para>
    /// </remarks>
    public void Finish()
    {
        if (!StepQueue.IsAddingCompleted)
            StepQueue.CompleteAdding();
    }

    /// <summary>
    /// Attempts to retrieve and remove the next step from the queue for processing.
    /// </summary>
    /// <remarks>
    /// This method blocks until a step becomes available in the queue or the operation is canceled.
    /// </remarks>
    /// <param name="step">When this method returns, contains the step retrieved from the queue if one was available; otherwise, <see langword="null"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> if a step was successfully retrieved from the queue; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    protected override bool TakeNextStep([NotNullWhen(true)] out IStep? step, CancellationToken cancellationToken)
    {
        return StepQueue.TryTake(out step, Timeout.Infinite, cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by the <see cref="ProducerConsumerStepRunner"/>.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; 
    /// <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing) 
            StepQueue.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Handles errors that occur during the execution of a step in the producer-consumer step runner.
    /// </summary>
    /// <param name="exception">The exception that was thrown during the execution of the step.</param>
    /// <param name="stepError">
    /// The <see cref="StepRunnerErrorEventArgs"/> containing details about the error, including the step
    /// that caused the error and whether the runner should cancel further processing.
    /// </param>
    /// <remarks>
    /// This method is invoked when an error occurs during the execution of a step. If the <see cref="StepRunnerErrorEventArgs.Cancel"/>
    /// property is set to <c>true</c>, the runner will terminate further processing by calling <see cref="Finish"/>.
    /// </remarks>
    protected override void OnError(Exception exception, StepRunnerErrorEventArgs stepError)
    {
        base.OnError(exception, stepError);
        if (stepError.Cancel)
            Finish();
    }

    /// <summary>
    /// Performs cleanup actions when the runner is requested to stop execution.
    /// </summary>
    /// <remarks>
    /// This method overrides the base implementation to ensure that the runner
    /// completes its processing by signaling that no more steps are expected.
    /// </remarks>
    protected override void OnRunnerStopped()
    {
        base.OnRunnerStopped();
        Finish();
    }
}