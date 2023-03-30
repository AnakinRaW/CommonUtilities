using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Runners;

/// <summary>
/// Runner engine, which executes all queued _steps parallel. Steps may be queued while step execution has been started.
/// The execution is finished only if <see cref="Finish"/> or <see cref="FinishAndWait"/> was called explicitly.
/// </summary>
public sealed class ParallelBlockingRunner : IParallelRunner
{
    /// <inheritdoc/>
    public event EventHandler<StepErrorEventArgs>? Error;

    private readonly ConcurrentBag<Exception> _exceptions;
    private readonly ConcurrentBag<IStep> _steps;
    private readonly ILogger? _logger;
    private readonly int _workerCount;
    private readonly Task[] _runnerTasks;
    private CancellationToken _cancel;

    private BlockingCollection<IStep> StepQueue { get; }

    /// <summary>
    /// Aggregates all step exceptions, if any happened.
    /// </summary>
    public AggregateException? Exception => _exceptions.Count > 0 ? new AggregateException(_exceptions) : null;

    internal bool IsCancelled { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="ParallelRunner"/> instance.
    /// </summary>
    /// <param name="workerCount">The number of parallel workers.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the number of workers is below 1.</exception>
    public ParallelBlockingRunner(int workerCount, IServiceProvider serviceProvider)
    {
        if (workerCount is < 1 or >= 64)
            throw new ArgumentException("invalid parallel worker count");
        _workerCount = workerCount;
        _runnerTasks = new Task[_workerCount];
        _steps = new ConcurrentBag<IStep>();
        StepQueue = new BlockingCollection<IStep>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _exceptions = new ConcurrentBag<Exception>();
    }

    /// <inheritdoc/>
    public void Run(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _cancel = token;
        for (var index = 0; index < _workerCount; ++index)
            _runnerTasks[index] = Task.Factory.StartNew(RunThreaded, CancellationToken.None);
    }

    /// <inheritdoc/>
    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
        var exception = Exception;
        if (exception != null)
            throw exception;
    }

    /// <inheritdoc/>
    public void Wait(TimeSpan timeout)
    {
        if (!Task.WaitAll(_runnerTasks, timeout))
            throw new TimeoutException();
    }

    /// <summary>
    /// Signals, this instance does not expect any more steps.
    /// </summary>
    public void Finish()
    {
        StepQueue.CompleteAdding();
    }

    /// <summary>
    /// Signals, this instance does not expect any more steps and waits for finished execution.
    /// </summary>
    /// <param name="throwsException"></param>
    public void FinishAndWait(bool throwsException = false)
    {
        Finish();
        try
        {
            Wait();
        }
        catch
        {
            if (throwsException)
                throw;
        }
    }

    /// <inheritdoc/>
    public void Queue(IStep step)
    {
        if (step is null)
            throw new ArgumentNullException(nameof(step));
        StepQueue.Add(step, CancellationToken.None);
    }

    /// <inheritdoc/>
    public IEnumerator<IStep> GetEnumerator()
    {
        return _steps.GetEnumerator();
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
                        _logger?.LogTrace($"Activity threw exception {ex.GetType()}: {ex.Message}" + Environment.NewLine + $"{ex.StackTrace}");
                    else
                        _logger?.LogTrace(ex, $"Activity threw exception {ex.GetType()}: {ex.Message}");
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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void OnError(StepErrorEventArgs e)
    {
        Error?.Invoke(this, e);
        if (!e.Cancel)
            return;
        IsCancelled |= e.Cancel;
    }
}