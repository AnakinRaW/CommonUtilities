﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued <see cref="IStep"/> sequentially in the order they are queued. 
/// </summary>
public class StepRunner : IStepRunner
{
    /// <inheritdoc/>
    public event EventHandler<StepErrorEventArgs>? Error;

    /// <summary>
    /// Gets a modifiable bag of all executed steps.
    /// </summary>
    protected readonly ConcurrentBag<IStep> ExecutedStepsBag = new();

    /// <summary>
    /// Gets the queue of all to be performed steps.
    /// </summary>
    protected ConcurrentQueue<IStep> StepQueue { get; }

    /// <summary>
    /// Gets the logger instance of this stepRunner.
    /// </summary>
    protected ILogger? Logger { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<IStep> ExecutedSteps => ExecutedStepsBag.ToArray();

    internal bool IsCancelled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StepRunner"/> class.
    /// </summary>
    /// <param name="services">The service provider for this instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public StepRunner(IServiceProvider services)
    {
        if (services == null) 
            throw new ArgumentNullException(nameof(services));
        StepQueue = new ConcurrentQueue<IStep>();
        Logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <inheritdoc/>
    public virtual Task RunAsync(CancellationToken token)
    {
        return Task.Run(() =>
        {
            Invoke(token);
        }, CancellationToken.None);
    }

    /// <inheritdoc/>
    public void AddStep(IStep step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));
        StepQueue.Enqueue(step);
    }

    /// <summary>
    /// Sequentially runs all queued steps. Faulted steps will raise the <see cref="Error"/> event.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected virtual void Invoke(CancellationToken token)
    {
        var alreadyCancelled = false;

        while (StepQueue.TryDequeue(out var step))
        {
            try
            {
                ThrowIfCancelled(token);

                ExecutedStepsBag.Add(step);
                step.Run(token);
            }
            catch (StopRunnerException)
            {
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

                var error = new StepErrorEventArgs(step)
                {
                    Cancel = token.IsCancellationRequested || IsCancelled ||
                             e.IsExceptionType<OperationCanceledException>()
                };
                if (error.Cancel)
                    alreadyCancelled = true;
                OnError(error);
            }
        }
    }

    /// <summary>
    /// Raises the <see cref="Error"/> event.
    /// </summary>
    /// <param name="e">The event args to use.</param>
    protected virtual void OnError(StepErrorEventArgs e)
    {
        Error?.Invoke(this, e);
        if (!e.Cancel)
            return;
        IsCancelled |= e.Cancel;
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
}