using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

public abstract class StepRunnerPipelineBase<TStepRunner>(IServiceProvider serviceProvider) : Pipeline(serviceProvider) where TStepRunner : IStepRunner
{
    private TStepRunner? _stepRunner;

    protected internal TStepRunner StepRunner =>
        _stepRunner ?? throw new InvalidOperationException("Step runner not initialized. Ensure PrepareAsync has been called.");

    protected internal bool IsStepRunnerInitialized => _stepRunner != null;

    /// <summary>
    /// Gets a value indicating the pipeline shall abort execution on the first received error.
    /// </summary>
    public bool FailFast { get; protected set; } = false;

    protected abstract TStepRunner CreateRunner();

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

    protected override async Task ExecuteAsync(CancellationToken token)
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

    protected virtual void OnError(object sender, StepRunnerErrorEventArgs e)
    {
        if (!e.Cancel)
            PipelineFailed = true;

        if (FailFast || e.Cancel)
            Cancel();
    }

    protected override void DisposeResources()
    {
        base.DisposeResources();
        if (IsStepRunnerInitialized && StepRunner is IDisposable disposableRunner)
            disposableRunner.Dispose();
    }
}