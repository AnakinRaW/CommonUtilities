﻿using System;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// An awaitable step implementation.
/// </summary>
public abstract class SynchronizedStep : PipelineStep
{
    /// <summary>
    /// Event gets raised if this instance failed with an <see cref="OperationCanceledException"/>.
    /// </summary>
    public event EventHandler<EventArgs>? Canceled;

    private readonly ManualResetEvent _handle;

    /// <summary>
    /// Initializes a new <see cref="SynchronizedStep"/>.
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected SynchronizedStep(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _handle = new ManualResetEvent(false);
    }

    /// <inheritdoc/>
    ~SynchronizedStep()
    {
        Dispose(false);
    }

    /// <summary>
    ///  Waits until the predefined runner has finished.
    /// </summary>
    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Waits until the predefined runner has finished.
    /// </summary>
    /// <param name="timeout">The time duration to wait.</param>
    /// <exception cref="TimeoutException">If <paramref name="timeout"/>.</exception>
    public void Wait(TimeSpan timeout)
    {
        if (!_handle.WaitOne(timeout))
            throw new TimeoutException();
    }

    /// <summary>
    /// Executes this step.
    /// </summary>
    /// <param name="token"></param>
    protected abstract void SynchronizedInvoke(CancellationToken token);

    /// <summary>
    /// Disposes managed resources of this instance.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> is this instance gets disposed; <see langword="false"/> if it get's finalized.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _handle.Dispose();
        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected sealed override void RunCore(CancellationToken token)
    {
        try
        {
            SynchronizedInvoke(token);
        }
        catch (Exception ex)
        {
            if (ex.IsExceptionType<OperationCanceledException>()) 
                Canceled?.Invoke(this, EventArgs.Empty);
            throw;
        }
        finally
        {
            _handle.Set();
        }
    }
}