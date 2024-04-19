using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// 
/// </summary>
public abstract class AsyncStep : DisposableObject, IStep
{
    private readonly TaskCompletionSource<Task> _taskCompletion = new();

    /// <summary>
    /// Gets the service provider of this step.
    /// </summary>
    protected IServiceProvider Services { get; }

    /// <summary>
    /// Gets the logger of this step.
    /// </summary>
    protected ILogger? Logger { get; }


    /// <inheritdoc />
    public Exception? Error
    {
        get
        {
            if (_taskCompletion.Task.IsFaulted)
                return _taskCompletion.Task.Exception?.InnerException;

            if (!_taskCompletion.Task.IsCompleted) 
                return null;

            if (_taskCompletion.Task.Result.IsFaulted)
                return _taskCompletion.Task.Result.Exception?.InnerException;

            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected AsyncStep(IServiceProvider serviceProvider)
    {
        Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public TaskAwaiter GetAwaiter()
    {
        if (_taskCompletion.Task.IsCompleted)
            return _taskCompletion.Task.Result.GetAwaiter();

        return Task.Run(async () =>
        {
            var task = await _taskCompletion.Task.ConfigureAwait(false);
            await task.ConfigureAwait(false);
        }).GetAwaiter();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    protected abstract Task RunAsync(CancellationToken token);

    /// <inheritdoc />
    public void Run(CancellationToken token)
    {
        Logger?.LogTrace($"BEGIN on thread-pool: {this}");
        var task = RunAsync(token);
        _taskCompletion.SetResult(task);
    }
}