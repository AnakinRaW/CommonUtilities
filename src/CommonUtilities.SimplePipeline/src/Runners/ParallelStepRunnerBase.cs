using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Base class for an <see cref="IStepRunner"/> that allows parallel step execution on the thread pool.
/// </summary>
public abstract class ParallelStepRunnerBase : StepRunnerBase, IParallelStepRunner
{
    private readonly ConcurrentBag<Exception> _exceptions;
    private readonly Task[] _tasks;

    /// <summary>
    /// Gets the number of parallel workers.
    /// </summary>
    public int WorkerCount { get; }

    /// <inheritdoc />
    public AggregateException? Exception => _exceptions.Count > 0 ? new AggregateException(_exceptions) : null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelStepRunner"/> class with the specified number of workers.
    /// </summary>
    /// <param name="workerCount">The number of parallel workers.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the number of workers is below 1 or above 64.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected ParallelStepRunnerBase(int workerCount, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        if (workerCount is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(workerCount));
        WorkerCount = workerCount;
        _exceptions = [];
        _tasks = new Task[workerCount];
    }

    /// <inheritdoc/>
    public override Task RunAsync(CancellationToken token)
    {
        for (var index = 0; index < WorkerCount; ++index)
            _tasks[index] = Task.Factory.StartNew(() => RunSteps(token), TaskCreationOptions.LongRunning);
        return Task.WhenAll(_tasks);
    }

    /// <inheritdoc/>
    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc/>
    public void Wait(TimeSpan timeout)
    {
        if (!Task.WaitAll(_tasks, timeout))
            throw new TimeoutException();

        var exception = Exception;
        if (exception != null)
            throw exception;
    }

    /// <inheritdoc />
    protected override void OnError(Exception exception, StepErrorEventArgs? stepError)
    {
        _exceptions.Add(exception);
        base.OnError(exception, stepError);
    }
}