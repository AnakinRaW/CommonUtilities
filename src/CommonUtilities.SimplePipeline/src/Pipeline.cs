using System;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Base implementation for an <see cref="IPipeline"/>
/// </summary>
public abstract class Pipeline : DisposableObject, IPipeline
{
    private bool? _prepareSuccessful;

    /// <inheritdoc/>
    public bool Prepare()
    {
        if (IsDisposed)
            throw new ObjectDisposedException("Pipeline already disposed");
        if (_prepareSuccessful.HasValue)
            return _prepareSuccessful.Value;
        _prepareSuccessful = PrepareCore();
        return _prepareSuccessful.Value;
    }

    /// <inheritdoc/>
    public void Run(CancellationToken token = default)
    {
        if (IsDisposed)
            throw new ObjectDisposedException("Pipeline already disposed");
        token.ThrowIfCancellationRequested();
        if (!Prepare())
            return;
        RunCore(token);
    }

    /// <summary>
    /// Performs the actual preparation of this instance.
    /// </summary>
    /// <returns><see langword="true"/> if the planning was successful; <see langword="false"/> otherwise.</returns>
    protected abstract bool PrepareCore();

    /// <summary>
    /// Implements the run logic of this instance.
    /// </summary>
    /// <remarks>It's assured this instance is already prepared when this method gets called.</remarks>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected abstract void RunCore(CancellationToken token);
}