using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
public abstract class StepRunnerPipelineBase(IServiceProvider serviceProvider) : Pipeline(serviceProvider)
{
    private IStepRunner? _stepRunner;

    protected internal IStepRunner StepRunner =>
        _stepRunner ?? throw new InvalidOperationException("Step runner not initialized. Ensure PrepareAsync has been called.");

    /// <summary>
    /// Gets a value indicating the pipeline shall abort execution on the first received error.
    /// </summary>
    public bool FailFast { get; protected set; } = false;

    protected abstract IStepRunner CreateRunner();

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

        ThrowIfAnyStepsFailed(StepRunner.ExecutedSteps);
    }

    protected void InitializeRunner()
    {
        if (_stepRunner is not null)
            return;
        _stepRunner = CreateRunner() ?? throw new InvalidOperationException("CreateRunner returned null!");
    }

    protected static void ThrowIfAnyStepsFailed(IEnumerable<IStep> steps)
    {
        var failedBuildSteps = steps.WhereFailed().ToList();
        if (failedBuildSteps.Count > 0)
            throw new StepFailureException(failedBuildSteps);
    }


    protected virtual void OnError(object sender, StepRunnerErrorEventArgs e)
    {
        if (!e.Cancel)
            PipelineFailed = true;
        
        if (FailFast || e.Cancel)
            Cancel();
    }


    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return GetType().Name;
    }
}