using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A pipeline that runs preparation and execution in parallel using a producer/consumer pattern.
/// </summary>
/// <remarks>
/// Steps are added to the runner while execution is already in progress.
/// Useful when preparation is work-intensive.
/// </remarks>
public abstract class ParallelProducerConsumerPipeline(int workerCount, IServiceProvider serviceProvider)
    : StepRunnerPipelineBase<ProducerConsumerStepRunner>(serviceProvider)
{
    private Exception? _preparationException;

    /// <summary>
    /// Builds the steps asynchronously as they become available.
    /// </summary>
    protected abstract IAsyncEnumerable<IStep> BuildStepsAsync(CancellationToken token);

    protected sealed override ProducerConsumerStepRunner CreateRunner()
    {
        return new ProducerConsumerStepRunner(workerCount, ServiceProvider);
    }

    /// <inheritdoc/>
    protected sealed override async Task PrepareCoreAsync(CancellationToken token)
    {
        InitializeRunner();
        await foreach (var step in BuildStepsAsync(token).ConfigureAwait(false)) 
            StepRunner.AddStep(step);
        StepRunner.Finish();
    }
    
    /// <inheritdoc/>
    protected sealed override async Task RunCoreAsync(CancellationToken token)
    {
        InitializeRunner();

        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        SetLinkedCancellationTokenSource(cts);

        if (!IsPrepared) 
            RunPreparationAsync(cts.Token).Forget();

        try
        {
            await ExecuteAsync(cts.Token).ConfigureAwait(false);
            cts.Token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            Cancelled = true;
            throw;
        }
        catch
        {
            Failed = true;
            throw;
        }
        finally
        {
            SetLinkedCancellationTokenSource(null);
            cts.Dispose();
        }
    }

    /// <inheritdoc/>
    protected sealed override async Task ExecuteAsync(CancellationToken token)
    {
        await base.ExecuteAsync(token).ConfigureAwait(false);

        if (_preparationException is not null)
            throw _preparationException;
    }

    private async Task RunPreparationAsync(CancellationToken token)
    {
        try
        {
            await WaitForPreparationAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Cancelled = true;
            Cancel();
        }
        catch (Exception e)
        {
            Failed = true;
            _preparationException = e;

            if (FailFast)
                Cancel();
        }
        finally
        {
            StepRunner.Finish();
        }
    }
}


