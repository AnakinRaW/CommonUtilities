using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A simple pipeline that runs all steps on the thread pool in parallel. Allows to run the pipeline even if preparation is not completed.
/// </summary>
/// <remarks>
/// Useful, if preparation is work intensive.
/// </remarks>
public abstract class ParallelProducerConsumerPipeline : DisposableObject, IPipeline
{
    private readonly bool _failFast;
    private CancellationTokenSource? _linkedCancellationTokenSource;
    private readonly ParallelProducerConsumerRunner _runner;

    private bool? _prepareSuccessful;

    /// <summary>
    /// Gets or sets a value indicating whether the pipeline has encountered a failure.
    /// </summary>
    protected bool PipelineFailed { get; set; }


    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelProducerConsumerPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection within the pipeline.</param>
    /// <param name="workerCount">The number of worker threads to be used for parallel execution.</param>
    /// <param name="failFast">A value indicating whether the pipeline should fail fast.</param>
    protected ParallelProducerConsumerPipeline(IServiceProvider serviceProvider, int workerCount = 4, bool failFast = true)
    {
        _failFast = failFast;
        _runner = new ParallelProducerConsumerRunner(workerCount, serviceProvider);
    }

    /// <inheritdoc/>
    public async Task<bool> PrepareAsync()
    {
        ThrowIfDisposed();
        if (_prepareSuccessful.HasValue)
            return _prepareSuccessful.Value;

        await BuildSteps(_runner).ConfigureAwait(false);
       
        _prepareSuccessful = true;
        return _prepareSuccessful.Value;
    }

    /// <inheritdoc/>
    public async Task RunAsync(CancellationToken token = default)
    {
        ThrowIfDisposed();
        token.ThrowIfCancellationRequested();

        if (_prepareSuccessful is false)
            return;

        try
        {
            _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            if (_prepareSuccessful is null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var result = await PrepareAsync().ConfigureAwait(false);
                        if (!result)
                            _linkedCancellationTokenSource?.Cancel();
                    }
                    finally
                    {
                        _runner.Finish();
                    }
                }, token).Forget();
            }



            _runner.Error += OnError;
            await _runner.RunAsync(_linkedCancellationTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            _runner.Error -= OnError;
            if (_linkedCancellationTokenSource is not null)
            {
                _linkedCancellationTokenSource.Dispose();
                _linkedCancellationTokenSource = null;
            }
        }

        if (!PipelineFailed && _prepareSuccessful.HasValue && _prepareSuccessful.Value)
            return;

        if (_prepareSuccessful is not true)
            throw new InvalidOperationException("Preparation of the pipeline failed.");

        var failedBuildSteps = _runner.Steps
            .Where(p => p.Error != null && !p.Error.IsExceptionType<OperationCanceledException>())
            .ToList();

        if (failedBuildSteps.Any())
            throw new StepFailureException(failedBuildSteps);
    }

    /// <summary>
    /// Builds the steps in the order they should be executed within the pipeline.
    /// </summary>
    /// <returns>A list of steps in the order they should be executed.</returns>
    protected abstract Task BuildSteps(IStepQueue queue);

    /// <summary>
    /// Called when an error occurs within a step.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnError(object sender, StepErrorEventArgs e)
    {
        PipelineFailed = true;
        if (_failFast || e.Cancel)
            _linkedCancellationTokenSource?.Cancel();
    }
}