using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Base implementation for an <see cref="IPipeline"/>
/// </summary>
public abstract class Pipeline : DisposableObject, IPipeline
{
    private bool? _prepareSuccessful;
    
    /// <inheritdoc/>
    public async Task<bool> PrepareAsync()
    {
        if (IsDisposed)
            throw new ObjectDisposedException("Pipeline already disposed");
        if (_prepareSuccessful.HasValue)
            return _prepareSuccessful.Value;
        _prepareSuccessful = await PrepareCoreAsync().ConfigureAwait(false);
        return _prepareSuccessful.Value;
    }

    /// <inheritdoc/>
    public async Task RunAsync(CancellationToken token = default)
    {
        if (IsDisposed)
            throw new ObjectDisposedException("Pipeline already disposed");
        token.ThrowIfCancellationRequested();
        if (!await PrepareAsync().ConfigureAwait(false))
            return;
        await RunCoreAsync(token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the actual preparation of this instance.
    /// </summary>
    /// <returns><see langword="true"/> if the planning was successful; <see langword="false"/> otherwise.</returns>
    protected abstract Task<bool> PrepareCoreAsync();

    /// <summary>
    /// Implements the run logic of this instance.
    /// </summary>
    /// <remarks>It's assured this instance is already prepared when this method gets called.</remarks>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected abstract Task RunCoreAsync(CancellationToken token);
}