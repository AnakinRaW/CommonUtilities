using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Base class for an <see cref="IStepRunner"/>.
/// </summary>
public abstract class StepRunnerBase : IStepRunner
{
    /// <inheritdoc/>
    public event EventHandler<StepRunnerErrorEventArgs>? Error;

    /// <summary>
    /// Gets a modifiable bag of all executed steps.
    /// </summary>
    protected readonly ConcurrentBag<IStep> ExecutedStepsBag = new();

    /// <summary>
    /// Gets the logger instance of this stepRunner.
    /// </summary>
    protected ILogger? Logger { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<IStep> ExecutedSteps => ExecutedStepsBag.ToArray();

    internal bool IsCancelled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StepRunnerBase"/> class.
    /// </summary>
    /// <param name="services">The service provider for this instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    protected StepRunnerBase(IServiceProvider services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        Logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <inheritdoc/>
    public abstract Task RunAsync(CancellationToken token);

    /// <inheritdoc/>
    public abstract void AddStep(IStep step);

    /// <summary>
    /// Tries to get the next step from the step queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="step">When this method returns, contains the next step to execute, or <see langword="null"/> if there is no step to execute.</param>
    /// <returns><see langword="true"/> when there exists a next step; otherwise, <see langword="false"/>.</returns>
    protected abstract bool TakeNextStep(CancellationToken cancellationToken, [NotNullWhen(true)] out IStep? step);

    /// <summary>
    /// Takes steps from the step queue and executes it.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    protected void RunSteps(CancellationToken token)
    {
        var alreadyCancelled = false;
        try
        {
            while (TakeNextStep(token, out var step))
            {
                try
                {
                    ThrowIfCancelled(token);

                    ExecutedStepsBag.Add(step);
                    step.Run(token);
                }
                catch (StopRunnerException)
                {
                    OnRunnerStopped();
                    Logger?.LogTrace("Stop subsequent steps");
                    break;
                }
                catch (Exception e)
                {
                    if (!alreadyCancelled)
                    {
                        if (e.IsExceptionType<OperationCanceledException>())
                            Logger?.LogTrace($"Step {step} cancelled");
                        else
                            Logger?.LogTrace(e, $"Step {step} threw an exception: {e.GetType()}: {e.Message}");
                    }

                    var error = new StepRunnerErrorEventArgs(e, step)
                    {
                        Cancel = token.IsCancellationRequested || IsCancelled || e.IsExceptionType<OperationCanceledException>()
                    };
                    if (error.Cancel)
                        alreadyCancelled = true;
                    OnError(e, error);
                }
            }
        }
        catch (OperationCanceledException e)
        {
            OnError(e, new StepRunnerErrorEventArgs(e, null));
            IsCancelled = true;
        }
    }

    /// <summary>
    /// Allows an overriding class to handle step errors and raises the <see cref="Error"/> event.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="stepError">The event args to use.</param>
    protected virtual void OnError(Exception exception, StepRunnerErrorEventArgs stepError)
    {
        Error?.Invoke(this, stepError);
        if (!stepError.Cancel)
            return;
        IsCancelled |= stepError.Cancel;
    }

    /// <summary>
    /// Throws an <see cref="OperationCanceledException"/> if the given token was requested for cancellation.
    /// </summary>
    /// <param name="token">The token to check for cancellation.</param>
    /// <exception cref="OperationCanceledException">If the token was requested for cancellation.</exception>
    protected void ThrowIfCancelled(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (IsCancelled)
            throw new OperationCanceledException(token);
    }

    /// <summary>
    /// Allows an overriding class to perform cleanup actions once the runner was requested to stop execution.
    /// </summary>
    protected virtual void OnRunnerStopped()
    {
    }
}