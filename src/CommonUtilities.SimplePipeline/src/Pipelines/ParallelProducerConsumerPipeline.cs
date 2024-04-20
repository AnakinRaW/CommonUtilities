using System;
using System.Collections.Generic;
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
public abstract class ParallelProducerConsumerPipeline : Pipeline
{
    private readonly bool _failFast;
    private CancellationTokenSource? _linkedCancellationTokenSource;
    private readonly ParallelProducerConsumerRunner _runner;

    private Exception? _preparationException;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelProducerConsumerPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection within the pipeline.</param>
    /// <param name="workerCount">The number of worker threads to be used for parallel execution.</param>
    /// <param name="failFast">A value indicating whether the pipeline should fail fast.</param>
    protected ParallelProducerConsumerPipeline(int workerCount, bool failFast, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _failFast = failFast;
        _runner = new ParallelProducerConsumerRunner(workerCount, serviceProvider);
    }

    /// <inheritdoc/>
    public sealed override async Task RunAsync(CancellationToken token = default)
    {
        ThrowIfDisposed();
        token.ThrowIfCancellationRequested();

        if (PrepareSuccessful is false)
            return;

        _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        if (PrepareSuccessful is null)
        {
            Task.Run(async () =>
            {
                try
                {
                    var result = await PrepareAsync().ConfigureAwait(false);
                    if (!result)
                    {
                        PipelineFailed = true;
                        _linkedCancellationTokenSource?.Cancel();
                    }
                }
                catch (Exception e)
                {
                    PipelineFailed = true;
                    _preparationException = e;
                }
                finally
                {
                    _runner.Finish();
                }
            }, token).Forget();
        }

        try
        {
            await RunCoreAsync(_linkedCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            PipelineFailed = true;
            throw;
        }
    }

    /// <summary>
    /// Builds the steps in the order they should be executed within the pipeline.
    /// </summary>
    /// <returns>A list of steps in the order they should be executed.</returns>
    protected abstract IAsyncEnumerable<IStep> BuildSteps();

    /// <inheritdoc/>
    protected override async Task<bool> PrepareCoreAsync()
    {
        await foreach (var step in BuildSteps().ConfigureAwait(false)) 
            _runner.AddStep(step);
        return true;
    }

    /// <inheritdoc/>
    protected override async Task RunCoreAsync(CancellationToken token)
    {
        try
        {
            _runner.Error += OnError;
            await _runner.RunAsync(token).ConfigureAwait(false);
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

        if (!PipelineFailed)
            return;

        if (_preparationException is not null)
            throw _preparationException;

        ThrowIfAnyStepsFailed(_runner.Steps);
    }

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

    /// <inheritdoc />
    protected override void DisposeManagedResources()
    {
        base.DisposeManagedResources();
        _runner.Dispose();
    }
}