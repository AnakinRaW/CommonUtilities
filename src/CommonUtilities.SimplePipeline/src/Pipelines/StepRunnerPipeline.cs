using System;
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
/// use <see cref="ParallelProducerConsumerPipeline "/> instead.
/// </para>
/// </remarks>
public abstract class StepRunnerPipeline(IServiceProvider serviceProvider) : StepRunnerPipelineBase<IStepRunner>(serviceProvider)
{ 
    protected abstract Task PrepareRunnerAsync(CancellationToken token);

    protected sealed override async Task PrepareCoreAsync(CancellationToken token)
    {
        InitializeRunner();
        await PrepareRunnerAsync(token).ConfigureAwait(false);
    }

    protected sealed override Task RunCoreAsync(CancellationToken token)
    {
        return base.RunCoreAsync(token);
    }

    protected sealed override Task ExecuteAsync(CancellationToken token)
    {
        return base.ExecuteAsync(token);
    }
}