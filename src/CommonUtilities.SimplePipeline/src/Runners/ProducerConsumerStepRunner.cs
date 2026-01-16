using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
/// <para>
/// <b>Thread Safety:</b> While this class supports adding steps while the runner is executing, it is <b>not</b> 
/// thread-safe by design. Adding steps from different threads or concurrently with cancellation/error handling 
/// may lead to race conditions where the consequence might be that steps added after cancellation or an error
/// might still get scheduled for execution.
/// </para>
/// </remarks>
public class ProducerConsumerStepRunner(int workerCount, IServiceProvider serviceProvider)
    : AsyncStepRunner(workerCount, serviceProvider)
{
    private readonly Channel<IStep> _stepChannel = Channel.CreateUnbounded<IStep>();

    /// <summary>
    /// Adds a step to the runner for execution.
    /// </summary>
    /// <param name="step">The step to add to the runner.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="step"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the runner has already been finished and cannot accept new steps.</exception>
    public override void AddStep(IStep step)
    {
        if (!TryAddStep(step))
            throw new InvalidOperationException("Runner has been finished.");
    }

    /// <summary>
    /// Attempts to add a step to the runner for execution.
    /// </summary>
    /// <param name="step">The step to add to the runner.</param>
    /// <returns>
    /// <see langword="true"/> if the step was successfully added; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="step"/> is <see langword="null"/>.</exception>
    public bool TryAddStep(IStep step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));
        return _stepChannel.Writer.TryWrite(step);
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
        _stepChannel.Writer.TryComplete();
    }

    /// <summary>
    /// Asynchronously retrieves the next step to be executed from the internal queue.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the next <see cref="IStep"/> to be executed,
    /// or <see langword="null"/> if no more steps are available.
    /// </returns>
    /// <remarks>
    /// This method waits for a step to become available in the queue. If the queue is empty and no more steps will be added,
    /// it returns <see langword="null"/>. The operation can be cancelled by the provided <paramref name="cancellationToken"/>.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The operation is cancelled via the <paramref name="cancellationToken"/>.</exception>
    protected override async ValueTask<IStep?> TakeNextStepAsync(CancellationToken cancellationToken)
    {
        while (await _stepChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_stepChannel.Reader.TryRead(out var step))
                return step;
        }
        return null;
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