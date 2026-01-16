using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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

    /// <summary>
    /// Returns the service provider of this step.
    /// </summary>
    protected readonly IServiceProvider Services;

    /// <summary>
    /// Returns the logger of this step.
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    /// Gets the exception that occurred during the execution of the step,
    /// or <see langword="null"/> if the step completed successfully.
    /// </summary>
    /// <remarks>
    /// If the step is cancelled by an <see cref="OperationCanceledException"/>
    /// (which may also be wrapped inside an <see cref="AggregateException"/>),
    /// this property contains the underlying cause of the cancellation when available,
    /// otherwise it may be <see langword="null"/>.
    /// For all other failures, this property contains the exception that caused
    /// the step to fail.
    /// </remarks>
    public Exception? Error { get; internal set; }

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
        return task;
    }

    /// <inheritdoc />
    public TaskAwaiter GetAwaiter()
    {
        var tcsTask = _completionSource.Task;
        return tcsTask is { IsCompleted: true, Status: TaskStatus.RanToCompletion } 
            ? tcsTask.Result.GetAwaiter() 
            : GetAwaitableTask().GetAwaiter();
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

    private async Task GetAwaitableTask()
    {
        var task = await _completionSource.Task.ConfigureAwait(false);
        await task.ConfigureAwait(false);
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
            throw;
        }
        catch (StopRunnerException)
        {
            throw;
        }
        catch (AggregateException e)
        {
            if (!e.IsExceptionType<OperationCanceledException>())
            {
                Error = e;
                LogFaultException(e);
            }
            else
                Error = e.InnerExceptions.FirstOrDefault(p => p.IsExceptionType<OperationCanceledException>())?.InnerException;
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