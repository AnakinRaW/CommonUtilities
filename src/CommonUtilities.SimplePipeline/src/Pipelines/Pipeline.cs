using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Base implementation for an <see cref="IPipeline"/>.
/// </summary>
public abstract class Pipeline : DisposableObject, IPipeline
{
    private Task? _preparationTask;
    private Task? _runTask;
    private readonly object _lock = new();

    private CancellationTokenSource? _linkedCancellationTokenSource;
    private readonly object _cancellationLock = new();

    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger? Logger;

    protected bool IsPrepared =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        _preparationTask is { Status: TaskStatus.RanToCompletion, IsCompleted: true };
#else
        _preparationTask?.IsCompletedSuccessfully == true;
#endif

    public bool Failed { get; protected set; }
    
    public bool Cancelled { get; protected set; }
    
    protected Pipeline(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <inheritdoc/>
    public Task PrepareAsync(CancellationToken token = default)
    {
        ThrowIfDisposed();
        token.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (_preparationTask is not null)
                throw new InvalidOperationException("Pipeline preparation has already been started.");

            try
            {
                _preparationTask = PrepareCoreAsync(token);
            }
            catch (Exception ex)
            {
                _preparationTask = Task.FromException(ex);
                throw;
            }
            return _preparationTask;
        }
    }

    /// <inheritdoc/>
    public Task RunAsync(CancellationToken token = default)
    {
        ThrowIfDisposed();
        lock (_lock)
        {
            if (_runTask is not null)
                throw new InvalidOperationException("Pipeline has already been started.");

            _runTask = RunCoreAsync(token);
            return _runTask;
        }
    }

    /// <inheritdoc />
    public void Cancel()
    {
        lock (_cancellationLock)
        {
            if (_linkedCancellationTokenSource != null)
            {
                _linkedCancellationTokenSource.Cancel();
                Cancelled = true;
            }
        }
    }

    protected void SetLinkedCancellationTokenSource(CancellationTokenSource? cts)
    {
        lock (_cancellationLock)
        {
            _linkedCancellationTokenSource = cts;
        }
    }

    /// <summary>
    /// Performs the actual preparation of this instance.
    /// </summary>
    protected abstract Task PrepareCoreAsync(CancellationToken token);

    /// <summary>
    /// Orchestrates and executes the pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    ///  Override this method to customize execution flow.
    /// </para>
    /// <para>
    /// The default implementation calls <see cref="PrepareAsync"/> if needed,
    /// sets up cancellation, and delegates to <see cref="ExecuteAsync"/>.
    /// </para>
    /// </remarks>
    protected virtual async Task RunCoreAsync(CancellationToken token)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        SetLinkedCancellationTokenSource(cts);
        
        try
        {
            await WaitForPreparationAsync(cts.Token).ConfigureAwait(false);
            await ExecuteAsync(cts.Token).ConfigureAwait(false);
            cts.Token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            Cancelled = true;
            throw;
        }
        catch
        {
            Failed = true;
            throw;
        }
        finally
        {
            SetLinkedCancellationTokenSource(null);
            cts.Dispose();
        }
    }

    /// <summary>
    /// Waits for preparation or starts it if not yet started.
    /// </summary>
    protected Task WaitForPreparationAsync(CancellationToken token)
    {
        Task task;

        lock (_lock)
        {
            _preparationTask ??= PrepareCoreAsync(token);
            task = _preparationTask;
        }
        return task.WaitAsync(token);

    }

    /// <summary>
    /// Implements the actual execution logic of this instance.
    /// </summary>
    /// <remarks>It's assured this instance is already prepared when this method gets called.</remarks>
    protected abstract Task ExecuteAsync(CancellationToken token);


    [ExcludeFromCodeCoverage]
    public override string ToString() => GetType().Name;
}