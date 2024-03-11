using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Base class for a simple pipeline implementation utilizing one runner and then waits for the execution to finish.
/// </summary>
/// <typeparam name="TRunner">The type of the step runner.</typeparam>
public abstract class SimplePipeline<TRunner> : Pipeline where TRunner : StepRunner
{
    /// <summary>
    /// The service provider within the pipeline.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;
    
    private readonly bool _failFast;

    private CancellationTokenSource? _linkedCancellationTokenSource;
    private TRunner _buildRunner = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the pipeline has encountered a failure.
    /// </summary>
    protected bool PipelineFailed { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePipeline{TRunner}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider the pipeline.</param>
    /// <param name="failFast">A value indicating whether the pipeline should fail fast.</param>
    /// <remarks>
    /// The <paramref name="failFast"/> parameter determines whether the pipeline should stop executing immediately upon encountering the first failure.
    /// </remarks>
    protected SimplePipeline(IServiceProvider serviceProvider, bool failFast = true)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
    /// Builds the steps in the order they should be executed within the pipeline.
    /// </summary>
    /// <returns>A list of steps in the order they should be executed.</returns>
    protected abstract IList<IStep> BuildStepsOrdered();

    /// <inheritdoc/>
    protected sealed override bool PrepareCore()
    {
        _buildRunner = CreateRunner() ?? throw new InvalidOperationException("RunnerFactory created null value!");
        var steps = BuildStepsOrdered();
        foreach (var step in steps)
            _buildRunner.Queue(step);
        return true;
    }

    /// <inheritdoc/>
    protected sealed override void RunCore(CancellationToken token)
    {
        try
        {
            _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            _buildRunner.Error += OnError;
            _buildRunner.Run(_linkedCancellationTokenSource.Token);

            OnRunning(_buildRunner);
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

        var failedBuildSteps = _buildRunner.Steps.Where(p => p.Error != null && !p.Error.IsExceptionType<OperationCanceledException>())
            .ToList();

        if (failedBuildSteps.Any())
            throw new StepFailureException(failedBuildSteps);
    }

    /// <summary>
    /// Called after the step runner was started.
    /// </summary>
    /// <param name="buildRunner">The step runner.</param>
    protected virtual void OnRunning(TRunner buildRunner)
    {
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
}