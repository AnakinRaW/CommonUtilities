using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Base implementation for an <see cref="IJob"/>
/// </summary>
public abstract class JobBase : IJob
{
    private bool? _planSuccessful;

    private bool _initialized;

    /// <inheritdoc/>
    public bool Plan()
    {
        if (_planSuccessful.HasValue)
            return _planSuccessful.Value;
        InitializeInternal();
        _planSuccessful = PlanCore();
        return _planSuccessful.Value;
    }

    /// <inheritdoc/>
    public void Run(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        InitializeInternal();
        if (!Plan())
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
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow job cancellation.</param>
    /// <remarks>It's assured this instance is already initialized and planned when this method gets called.</remarks>
    protected abstract void RunCore(CancellationToken token);

    /// <summary>
    /// Initializes this instance. 
    /// </summary>
    /// <remarks>This method only gets called once per instance.</remarks>
    protected virtual void Initialize()
    {
    }

    private void InitializeInternal()
    {
        if (_initialized)
            return;
        Initialize();
        _initialized = true;
    }
}