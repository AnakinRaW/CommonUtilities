using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// Base implementation for an <see cref="IStep"/>.
/// </summary>
public abstract class PipelineStep : DisposableObject, IStep
{
    private readonly TaskCompletionSource<Task> _completionSource = new();
    private Task? _exposedTask;

    /// <summary>
    /// Returns the service provider of this step.
    /// </summary>
    protected readonly IServiceProvider Services;

    /// <summary>
    /// Returns the logger of this step.
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    ///  Gets the exception that occurred during the execution of the step, if any.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the step is cancelled by an <see cref="OperationCanceledException"/>
    /// (which may also be wrapped inside an <see cref="AggregateException"/>),
    /// this property contains the underlying cause of the cancellation when available,
    /// otherwise it may be <see langword="null"/>.
    /// </para>
    /// <para>
    /// If the step throws a <see cref="StopRunnerException"/>, this property is always <see langword="null"/>.
    /// This is because a <see cref="StopRunnerException"/> does not indicate a failure of the step itself.
    /// </para>
    /// <para>
    /// For all other failures, this property contains the exception that caused
    /// the step to fail.
    /// </para>
    /// </remarks>
    public Exception? Error { get; private set; }

    /// <inheritdoc/>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineStep"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected PipelineStep(IServiceProvider serviceProvider)
    {
        Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <inheritdoc/>
    public Task RunAsync(CancellationToken token)
    {
        var task = ExecuteStepAsync(token);
        _completionSource.TrySetResult(task);
        return GetStepTask();
    }

    /// <inheritdoc />
    public TaskAwaiter GetAwaiter()
    {
        return GetStepTask().GetAwaiter();
    }

    /// <inheritdoc/>
    public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
    {
        return GetStepTask().ConfigureAwait(continueOnCapturedContext);
    }

    /// <summary>
    /// Returns a string that represents the current <see cref="PipelineStep"/> instance.
    /// </summary>
    /// <returns>
    /// A string that represents the current <see cref="PipelineStep"/> instance, typically the name of the step's type.
    /// </returns>
    public override string ToString()
    {
        return GetType().Name;
    }

    /// <summary>0
    /// Executes this step. 
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected abstract Task RunCoreAsync(CancellationToken token);

    private async Task CreateAwaitableTask()
    {
        var task = await _completionSource.Task.ConfigureAwait(false);
        await task.ConfigureAwait(false);
    }

    private Task GetStepTask()
    {
        var existing = Volatile.Read(ref _exposedTask);
        if (existing is not null)
            return existing;

        var tcsTask = _completionSource.Task;
        if (tcsTask is { IsCompleted: true, Status: TaskStatus.RanToCompletion })
        {
            var result = tcsTask.Result;
            var original = Interlocked.CompareExchange(ref _exposedTask, result, null);
            return original ?? result;
        }

        var newTask = CreateAwaitableTask();
        var prev = Interlocked.CompareExchange(ref _exposedTask, newTask, null);
        return prev ?? newTask;
    }

    private async Task ExecuteStepAsync(CancellationToken token)
    {
        Logger?.LogTrace("BEGIN: {Step}", this);
        try
        {
            await RunCoreAsync(token).ConfigureAwait(false);
            Logger?.LogTrace("END: {Step}", this);
        }
        catch (OperationCanceledException ex)
        {
            Error = ex.InnerException;
            IsCancelled = true;
            throw;
        }
        catch (StopRunnerException)
        {
            throw;
        }
        catch (AggregateException e)
        {
            if (e.IsExceptionType<OperationCanceledException>())
            {
                Error = e.FindException<OperationCanceledException>()?.InnerException;
                IsCancelled = true;
            }
            else
            {
                Error = e;
                LogFaultException(e);
            }

            throw;
        }
        catch (Exception e)
        {
            Error = e;
            LogFaultException(e);
            throw;
        }
    }

    private void LogFaultException(Exception ex)
    { 
        Logger?.LogError(ex, ex.InnerException?.Message ?? ex.Message);
    }
}