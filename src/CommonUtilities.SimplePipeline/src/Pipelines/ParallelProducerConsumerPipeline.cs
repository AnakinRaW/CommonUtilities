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
public abstract class ParallelProducerConsumerPipeline : StepRunnerPipelineBase<ProducerConsumerStepRunner>
{
    private Exception? _preparationException;
    private readonly int _workerCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelProducerConsumerPipeline"/> class with the specified worker count and service provider.
    /// </summary>
    /// <param name="workerCount">The number of workers to be used in the pipeline. Must be between 1 and 64 inclusive.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve dependencies for the pipeline.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="workerCount"/> is less than 1 or greater than 64.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected ParallelProducerConsumerPipeline(int workerCount, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        if (workerCount is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(workerCount), "worker count must be between 1 and 64 inclusive");
        _workerCount = workerCount;
    }

    /// <summary>
    /// Asynchronously builds a collection of steps to be executed by the pipeline.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the steps to be built.</param>
    /// <returns>An asynchronous enumerable of <see cref="IStep"/> instances representing the steps to be executed.</returns>
    /// <remarks>
    /// This method is intended to be overridden in derived classes to provide the logic for preparing the steps
    /// that will be executed by the pipeline. The steps are produced asynchronously, allowing for efficient
    /// preparation of steps in scenarios where preparation is computationally intensive or involves I/O operations.
    /// </remarks>
    protected abstract IAsyncEnumerable<IStep> BuildStepsAsync(CancellationToken token);

    /// <summary>
    /// Creates an instance of <see cref="IStepRunner"/> to execute the steps in the pipeline using a producer/consumer pattern.
    /// </summary>
    /// <remarks>
    /// The <see cref="ProducerConsumerStepRunner"/> is initialized with the specified number of workers and the service provider.
    /// This method ensures that the runner is properly configured to handle the producer/consumer pattern for step execution.
    /// </remarks>
    /// <returns>
    /// A configured instance of <see cref="ProducerConsumerStepRunner"/>.
    /// </returns>
    protected sealed override ProducerConsumerStepRunner CreateRunner()
    {
        return new ProducerConsumerStepRunner(_workerCount, ServiceProvider);
    }

    /// <summary>
    /// Prepares the pipeline by initializing the step runner and adding steps to it asynchronously.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the preparation to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous preparation operation.</returns>
    /// <remarks>
    /// This method initializes the step runner and asynchronously builds and adds steps to the runner.
    /// Once all steps are added, the runner is marked as finished.
    /// </remarks>
    protected sealed override async Task PrepareCoreAsync(CancellationToken token)
    {
        _ = StepRunner;
        await foreach (var step in BuildStepsAsync(token).ConfigureAwait(false)) 
            StepRunner.AddStep(step);
        StepRunner.Finish();
    }
    
    /// <summary>
    /// Executes the core logic of the pipeline asynchronously.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method initializes the runner, links the provided cancellation token, and ensures the pipeline gets prepared.
    /// It handles cancellation and failure scenarios, ensuring proper cleanup of resources.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="Exception">Thrown when an error occurs during execution.</exception>
    protected sealed override async Task RunCoreAsync(CancellationToken token)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        var linkedToken = CancellationTokenSource.Token;

        if (!IsPrepared)
        {
            Task.Run(() => RunPreparationAsync(linkedToken), CancellationToken.None).Forget();
        }

        try
        {
            await ExecuteAsync(linkedToken).ConfigureAwait(false);
            linkedToken.ThrowIfCancellationRequested();
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
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Executes the pipeline asynchronously with the specified cancellation token.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous execution of the pipeline.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the provided <paramref name="token"/>.</exception>
    /// <exception cref="Exception">The exception that happened during preparation</exception>
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


