using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Base class for pipelines that use an <see cref="IStepRunner"/> with sequential preparation and execution.
/// </summary>
/// <remarks>
/// <para>
/// This class follows a sequential pattern: preparation completes fully before execution begins.
/// </para>
/// <para>
/// For pipelines that need to run preparation and execution in parallel (producer/consumer pattern),
/// use <see cref="ProducerConsumerPipeline "/> instead.
/// </para>
/// </remarks>
public abstract class StepRunnerPipeline(IServiceProvider serviceProvider) : StepRunnerPipelineBase<IStepRunner>(serviceProvider)
{ 
    /// <summary>
    /// Creates a collection of steps to be executed by the <see cref="IStepRunner"/>.
    /// </summary>
    /// <param name="token">
    /// A <see cref="CancellationToken"/> that can be used to cancel the step creation process.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of steps
    /// (<see cref="IStep"/>) to be added to the <see cref="IStepRunner"/>.
    /// </returns>
    /// <remarks>
    /// This method is abstract and must be implemented by derived classes to define the specific steps
    /// required for the pipeline. The steps are created during the preparation phase of the pipeline.
    /// </remarks>
    protected abstract Task<IList<IStep>> CreateRunnerSteps(CancellationToken token);

    /// <summary>
    /// Prepares the pipeline by initializing the step runner and adding the steps to be executed.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <remarks>
    /// This method initializes the step runner and sequentially adds the steps created by 
    /// <see cref="CreateRunnerSteps(CancellationToken)"/> to the runner. It ensures that the preparation phase
    /// is fully completed before the execution phase begins.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the step runner is not properly initialized before adding steps.
    /// </exception>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    protected sealed override async Task PrepareCoreAsync(CancellationToken token)
    {
        _ = StepRunner;
        var steps = await CreateRunnerSteps(token).ConfigureAwait(false);
        foreach (var step in steps) 
            StepRunner.AddStep(step);
    }

    /// <inheritdoc/>
    protected sealed override Task RunCoreAsync(CancellationToken token)
    {
        return base.RunCoreAsync(token);
    }
}