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
    private readonly ParallelProducerConsumerStepRunner _stepRunner;

    private Exception? _preparationException;

    /// <inheritdoc />
    protected override bool FailFast { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelProducerConsumerPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection within the pipeline.</param>
    /// <param name="workerCount">The number of worker threads to be used for parallel execution.</param>
    /// <param name="failFast">A value indicating whether the pipeline should fail fast.</param>
    protected ParallelProducerConsumerPipeline(int workerCount, bool failFast, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        FailFast = failFast;
        _stepRunner = new ParallelProducerConsumerStepRunner(workerCount, serviceProvider);
    }

    /// <inheritdoc/>
    public sealed override async Task RunAsync(CancellationToken token = default)
    {
        ThrowIfDisposed();

        LinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        if (!Prepared)
        {
            Task.Run(async () =>
            {
                try
                { 
                    await PrepareAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    PipelineFailed = true;
                    _preparationException = e;
                    
                    if (FailFast)
                        Cancel();
                }
                finally
                {
                    _stepRunner.Finish();
                }
            }, LinkedCancellationTokenSource.Token).Forget();
        }

        try
        {
            await RunCoreAsync(LinkedCancellationTokenSource.Token).ConfigureAwait(false);
            LinkedCancellationTokenSource.Token.ThrowIfCancellationRequested();
        }
        catch (Exception)
        {
            PipelineFailed = true;
            throw;
        }
        finally
        {
            if (LinkedCancellationTokenSource is not null)
            {
                LinkedCancellationTokenSource.Dispose();
                LinkedCancellationTokenSource = null;
            }
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
            _stepRunner.AddStep(step);
        _stepRunner.Finish();
        return true;
    }

    /// <inheritdoc/>
    protected override async Task RunCoreAsync(CancellationToken token)
    {
        try
        {
            _stepRunner.Error += OnError;
            await _stepRunner.RunAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _stepRunner.Error -= OnError;
        }

        if (!PipelineFailed)
            return;

        if (_preparationException is not null)
            throw _preparationException;

        ThrowIfAnyStepsFailed(_stepRunner.ExecutedSteps);
    }
}