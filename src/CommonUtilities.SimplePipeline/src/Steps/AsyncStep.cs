//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Runtime.CompilerServices;
//using System.Threading;
//using System.Threading.Tasks;

//namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

///// <summary>
///// A <see cref="IStep"/> that can be awaited on.
///// </summary>
//public abstract class AsyncStep : DisposableObject, IStep
//{
//    private readonly TaskCompletionSource<Task> _taskCompletion = new();

//    /// <summary>
//    /// Gets the service provider of this step.
//    /// </summary>
//    protected IServiceProvider Services { get; }

//    /// <summary>
//    /// Gets the logger of this step.
//    /// </summary>
//    protected ILogger? Logger { get; }

//    /// <inheritdoc />
//    public Exception? Error
//    {
//        get
//        {
//            if (_taskCompletion.Task.IsFaulted)
//                return _taskCompletion.Task.Exception?.InnerException;

//            if (!_taskCompletion.Task.IsCompleted) 
//                return null;

//            if (_taskCompletion.Task.Result.IsFaulted)
//                return _taskCompletion.Task.Result.Exception?.InnerException;

//            return null;
//        }
//    }

//    /// <summary>
//    /// Initializes a new instance of the <see cref="AsyncStep"/> class.
//    /// </summary>
//    /// <param name="serviceProvider">The service provider.</param>
//    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
//    protected AsyncStep(IServiceProvider serviceProvider)
//    {
//        Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
//        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
//    }

//    /// <summary>
//    /// Gets an awaiter used to await this <see cref="AsyncStep"/>.
//    /// </summary>
//    /// <returns>An awaiter instance.</returns>
//    public TaskAwaiter GetAwaiter()
//    {
//        if (_taskCompletion.Task.IsCompleted)
//            return _taskCompletion.Task.Result.GetAwaiter();

//        return Task.Run(async () =>
//        {
//            var task = await _taskCompletion.Task.ConfigureAwait(false);
//            await task.ConfigureAwait(false);
//        }).GetAwaiter();
//    }

//    /// <summary>
//    /// Run the step's action and returns the operation as a task reference.
//    /// </summary>
//    /// <param name="token"></param>
//    /// <returns>The task that represents the operation of this step.</returns>
//    protected abstract Task RunAsync(CancellationToken token);

//    /// <inheritdoc />
//    public void Run(CancellationToken token)
//    {
//        Logger?.LogTrace($"BEGIN on thread-pool: {this}");
//        var task = RunAsync(token);
//        _taskCompletion.SetResult(task);
//    }
//}