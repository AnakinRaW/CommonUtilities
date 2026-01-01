using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using System;
using System.Collections.Generic;
using System.Linq;
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
public abstract class ParallelProducerConsumerPipeline(
    int workerCount,
    IServiceProvider serviceProvider) : Pipeline(serviceProvider)
{
    private ProducerConsumerStepRunner? _stepRunner;
    private Exception? _preparationException;

    /// <summary>
    /// Gets a value indicating the pipeline shall abort execution on the first received error.
    /// </summary>
    public bool FailFast { get; protected set; } = false;

    private ProducerConsumerStepRunner StepRunner =>
        _stepRunner ?? throw new InvalidOperationException("Step runner not initialized.");

    /// <summary>
    /// Builds the steps asynchronously as they become available.
    /// </summary>
    protected abstract IAsyncEnumerable<IStep> BuildStepsAsync(CancellationToken token);

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
            PipelineCancelled = true;
            throw;
        }
        catch
        {
            PipelineFailed = true;
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
        try
        {
            StepRunner.Error += OnError!;
            await StepRunner.RunAsync(token).ConfigureAwait(false);
        }
        finally
        {
            StepRunner.Error -= OnError!;
        }

        if (_preparationException is not null)
            throw _preparationException;

        var failedSteps = StepRunner.ExecutedSteps.WhereFailed().ToList();
        if (failedSteps.Count > 0)
            throw new StepFailureException(failedSteps);
    }


    protected virtual void OnError(object sender, StepRunnerErrorEventArgs e)
    {
        if (FailFast || e.Cancel)
            Cancel();
    }

    private async Task RunPreparationAsync(CancellationToken token)
    {
        try
        {
            await WaitForPreparationAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            PipelineCancelled = true;
            Cancel();
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
            try
            {
                StepRunner.Finish();
            }
            catch (InvalidOperationException)
            {
                // Already finished or not initialized
            }
        }
    }

    private void InitializeRunner()
    {
        _stepRunner ??= new ProducerConsumerStepRunner(workerCount, ServiceProvider);
    }
}


