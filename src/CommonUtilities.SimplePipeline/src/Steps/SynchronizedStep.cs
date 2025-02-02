using System;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// A step that can be waited for.
/// </summary>
public abstract class SynchronizedStep : PipelineStep
{
    /// <summary>
    /// Event gets raised if this instance failed with an <see cref="OperationCanceledException"/>.
    /// </summary>
    public event EventHandler<EventArgs>? Canceled;

    private readonly ManualResetEvent _handle;

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedStep"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected SynchronizedStep(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _handle = new ManualResetEvent(false);
    }

    /// <summary>
    ///  Waits until the predefined stepRunner has finished.
    /// </summary>
    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Waits until the predefined stepRunner has finished.
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
    protected abstract void RunSynchronized(CancellationToken token);

    /// <inheritdoc />
    protected override void DisposeResources()
    {
        base.DisposeResources();
        _handle.Dispose();
    }

    /// <inheritdoc/>
    protected sealed override void RunCore(CancellationToken token)
    {
        try
        {
            RunSynchronized(token);
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