using System;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Base implementation for an <see cref="IPipeline"/>
/// </summary>
public abstract class PipelineBase : DisposableObject, IPipeline
{
    private bool? _prepareSuccessful;

    /// <inheritdoc/>
    public bool Prepare()
    {
        if (_prepareSuccessful.HasValue)
            return _prepareSuccessful.Value;
        _prepareSuccessful = PlanCore();
        return _prepareSuccessful.Value;
    }

    /// <inheritdoc/>
    public void Run(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!Prepare())
            return;
        RunCore(token);
    }

    /// <summary>
    /// Performs the actual planning of this instance.
    /// </summary>
    /// <returns><see langword="true"/> if the planning was successful; <see langword="false"/> otherwise.</returns>
    protected abstract bool PlanCore();

    /// <summary>
    /// Implements the run logic of this instance.
    /// </summary>
    /// <remarks>It's assured this instance is already prepared when this method gets called.</remarks>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected abstract void RunCore(CancellationToken token);
}