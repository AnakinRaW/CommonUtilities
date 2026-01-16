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

#if NET10_0_OR_GREATER
    private readonly Lock _reentryLock = new();
#else
    private readonly object _reentryLock = new();
#endif

    /// <summary>
    /// Gets the <see cref="System.Threading.CancellationTokenSource"/> used to cancel the execution of the pipeline.
    /// Returns <see langword="null"/> if the execution is not started or already finished.
    /// </summary>
    protected CancellationTokenSource? CancellationTokenSource;
    
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> for the pipeline.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;
    
    /// <summary>
    /// Gets the <see cref="ILogger"/> for the pipeline or <see langword="null"/> if no logger is registered.
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    /// Gets a value indicating whether the pipeline has been successfully prepared.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the pipeline preparation task has completed successfully; otherwise, <see langword="false"/>.
    /// </value>
    protected internal bool IsPrepared =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        _preparationTask is { Status: TaskStatus.RanToCompletion, IsCompleted: true };
#else
        _preparationTask?.IsCompletedSuccessfully is true;
#endif

    /// <summary>
    /// Gets a value indicating whether the pipeline has encountered a failure during its execution.
    /// </summary>
    /// <remarks>
    /// This property is set to <see langword="true"/> if an exception occurs during the execution of the pipeline.
    /// </remarks>
    public bool Failed { get; protected set; }
    
    /// <summary>
    /// Gets a value indicating whether the pipeline has been cancelled.
    /// </summary>
    /// <remarks>
    /// This property is set to <see langword="true"/> when the pipeline is explicitly cancelled
    /// or when an <see cref="OperationCanceledException"/> is thrown during execution.
    /// </remarks>
    public bool Cancelled { get; protected set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Pipeline"/> class with the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve dependencies for the pipeline.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected Pipeline(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <summary>
    /// Returns a string representation of the current <see cref="Pipeline"/> instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> that represents the name of the current pipeline type.
    /// </returns>
    [ExcludeFromCodeCoverage]
    public override string ToString() => GetType().Name;

    /// <summary>
    /// Prepares the pipeline for execution.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the preparation to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous preparation operation.</returns>
    /// <exception cref="InvalidOperationException">The pipeline already is prepared or preparation has been started.</exception>
    /// <exception cref="ObjectDisposedException">The pipeline has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The operation is canceled.</exception>
    public Task PrepareAsync(CancellationToken token = default)
    {
        ThrowIfDisposed();
        token.ThrowIfCancellationRequested();
        lock (_reentryLock)
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

    /// <summary>
    /// Executes the pipeline asynchronously.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the pipeline has already been started or is executing.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the pipeline has been disposed.</exception>
    public Task RunAsync(CancellationToken token = default)
    {
        ThrowIfDisposed();
        lock (_reentryLock)
        {
            if (_runTask is not null)
                throw new InvalidOperationException("Pipeline has already been started.");

            _runTask = RunCoreAsync(token);
            return _runTask;
        }
    }

    /// <summary>
    /// Cancels the execution of the pipeline.
    /// </summary>
    /// <remarks>
    /// This method ensures that the pipeline's execution is stopped by canceling the associated 
    /// <see cref="CancellationTokenSource"/>. Once canceled, the <see cref="Cancelled"/> property is set to <c>true</c>.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the pipeline or its associated resources have already been disposed.</exception>
    public void Cancel()
    {
        var cts = CancellationTokenSource;
        if (cts != null)
        {
            cts.Cancel();
            Cancelled = true;
        }
    }

    /// <summary>
    /// Performs the actual preparation of this instance.
    /// </summary>
    protected abstract Task PrepareCoreAsync(CancellationToken token);

    /// <summary>
    /// Implements the actual execution logic of this instance.
    /// </summary>
    /// <remarks>It's assured this instance is already prepared when this method gets called.</remarks>
    protected abstract Task ExecuteAsync(CancellationToken token);

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
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        
        try
        {
            await WaitForPreparationAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            await ExecuteAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            CancellationTokenSource.Token.ThrowIfCancellationRequested();
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
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Releases resources used by the pipeline, including any tasks and cancellation tokens.
    /// </summary>
    protected override void DisposeResources()
    {
        lock (_reentryLock)
        {
            _preparationTask?.Dispose();
            _runTask?.Dispose();
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = null;

            // Safe because we explicitly check the methods if disposed
            _preparationTask = null;
            _runTask = null;
        }

        base.DisposeResources();
    }

    /// <summary>
    /// Waits for preparation or starts it if not yet started.
    /// </summary>
    protected Task WaitForPreparationAsync(CancellationToken token)
    {
        Task task;

        lock (_reentryLock)
        {
            _preparationTask ??= PrepareCoreAsync(token);
            task = _preparationTask;
        }
        return task.WaitAsync(token);

    }
}