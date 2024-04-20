using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Base class for a simple pipeline implementation utilizing one <see cref="StepRunner"/>.
/// </summary>
/// <typeparam name="TRunner">The type of the step runner.</typeparam>
public abstract class SimplePipeline<TRunner> : Pipeline where TRunner : StepRunner
{
   private readonly bool _failFast;

    private CancellationTokenSource? _linkedCancellationTokenSource;
    private IRunner _buildRunner = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePipeline{TRunner}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider the pipeline.</param>
    /// <param name="failFast">A value indicating whether the pipeline should fail fast.</param>
    /// <remarks>
    /// The <paramref name="failFast"/> parameter determines whether the pipeline should stop executing immediately upon encountering the first failure.
    /// </remarks>
    protected SimplePipeline(IServiceProvider serviceProvider, bool failFast = true) : base(serviceProvider)
    {
        _failFast = failFast;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Creates the step runner for the pipeline.
    /// </summary>
    /// <returns>The step runner instance.</returns>
    protected abstract TRunner CreateRunner();

    /// <summary>
    /// Builds the steps that should be executed within the pipeline.
    /// </summary>
    /// <remarks>
    /// The order of the steps might be relevant, depending on the type of <typeparamref name="TRunner"/>.
    /// </remarks>
    /// <returns>A task that returns a list of steps.</returns>
    protected abstract Task<IList<IStep>> BuildSteps();

    /// <inheritdoc/>
    protected override async Task<bool> PrepareCoreAsync()
    {
        _buildRunner = CreateRunner() ?? throw new InvalidOperationException("RunnerFactory created null value!");
        var steps = await BuildSteps().ConfigureAwait(false);
        foreach (var step in steps)
            _buildRunner.AddStep(step);
        return true;
    }

    /// <inheritdoc/>
    protected override async Task RunCoreAsync(CancellationToken token)
    {
        try
        {
            _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            _buildRunner.Error += OnError;
            await _buildRunner.RunAsync(_linkedCancellationTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            _buildRunner.Error -= OnError;
            if (_linkedCancellationTokenSource is not null)
            {
                _linkedCancellationTokenSource.Dispose();
                _linkedCancellationTokenSource = null;
            }
        }

        if (!PipelineFailed)
            return;

        ThrowIfAnyStepsFailed(_buildRunner.Steps);
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
        _buildRunner.Dispose();
    }
}