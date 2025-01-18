using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued _steps parallel. Steps may be queued while step execution has been started.
/// The execution can finish only if <see cref="Finish"/> was called explicitly.
/// </summary>
public sealed class ParallelProducerConsumerStepRunner : ISynchronizedStepRunner
{ 
    /// <inheritdoc/>
    public event EventHandler<StepErrorEventArgs>? Error;

    private readonly ConcurrentBag<Exception> _exceptions;
    private readonly ConcurrentBag<IStep> _steps;
    private readonly ILogger? _logger;
    private readonly int _workerCount;
    private readonly Task[] _runnerTasks;
    private CancellationToken _cancel;

    /// <inheritdoc/>
    public IReadOnlyCollection<IStep> ExecutedSteps => _steps.ToArray();

    private BlockingCollection<IStep> StepQueue { get; }

    /// <summary>
    /// Gets an aggregated exception of all failed steps.
    /// </summary>
    public AggregateException? Exception => _exceptions.Count > 0 ? new AggregateException(_exceptions) : null;

    internal bool IsCancelled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelStepRunner"/> class with the specified number of workers.
    /// </summary>
    /// <param name="workerCount">The number of parallel workers.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the number of workers is below 1.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public ParallelProducerConsumerStepRunner(int workerCount, IServiceProvider serviceProvider)
    {
        if (workerCount is < 1 or >= 64)
            throw new ArgumentException("invalid parallel worker count");
        _workerCount = workerCount;
        _runnerTasks = new Task[_workerCount];
        _steps = [];
        StepQueue = new BlockingCollection<IStep>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _exceptions = [];
    }


    /// <inheritdoc/>
    public Task RunAsync(CancellationToken token)
    {
        _cancel = token;
        for (var index = 0; index < _workerCount; ++index)
            _runnerTasks[index] = Task.Factory.StartNew(RunThreaded, TaskCreationOptions.LongRunning);
        return Task.WhenAll(_runnerTasks); //.WaitAsync(token);
    }

    /// <inheritdoc/>
    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc/>
    public void Wait(TimeSpan timeout)
    {
        if (!Task.WaitAll(_runnerTasks, timeout))
            throw new TimeoutException();

        var exception = Exception;
        if (exception != null)
            throw exception;
    }

    /// <summary>
    /// Signals this instance does not expect any more steps.
    /// </summary>
    public void Finish()
    {
        StepQueue.CompleteAdding();
    }

    /// <inheritdoc/>
    public void AddStep(IStep step)
    {
        if (step is null)
            throw new ArgumentNullException(nameof(step));
        StepQueue.Add(step, CancellationToken.None);
    }

    private void RunThreaded()
    {
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancel);
        var canceled = false;
        foreach (var step in StepQueue.GetConsumingEnumerable())
        {
            try
            {
                _cancel.ThrowIfCancellationRequested();
                _steps.Add(step);
                step.Run(_cancel);
            }
            catch (StopRunnerException)
            {
                _logger?.LogTrace("Stop subsequent steps");
                StepQueue.CompleteAdding();
                break;
            }
            catch (Exception ex)
            {
                _exceptions.Add(ex);
                if (!canceled)
                {
                    if (ex.IsExceptionType<OperationCanceledException>())
                        _logger?.LogTrace($"Step threw exception {ex.GetType()}: {ex.Message}" + Environment.NewLine + $"{ex.StackTrace}");
                    else
                        _logger?.LogTrace(ex, $"Step threw exception {ex.GetType()}: {ex.Message}");
                }
                var e = new StepErrorEventArgs(step)
                {
                    Cancel = _cancel.IsCancellationRequested || IsCancelled || ex.IsExceptionType<OperationCanceledException>()
                };
                OnError(e);
                if (e.Cancel)
                {
                    canceled = true;
                    linkedTokenSource.Cancel();
                }
            }
        }
    }

    private void OnError(StepErrorEventArgs e)
    {
        Error?.Invoke(this, e);
        if (!e.Cancel)
            return;
        IsCancelled |= e.Cancel;
    }
}