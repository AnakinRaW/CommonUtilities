using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// Represents a pipeline step that executes a specific <see cref="IPipeline"/>.
/// </summary>
/// <remarks>
/// This step is responsible for running the provided pipeline and managing its lifecycle,
/// including handling exceptions and disposing of resources.
/// </remarks>
public sealed class RunPipelineStep : PipelineStep
{
    private readonly IPipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunPipelineStep"/> class.
    /// </summary>
    /// <param name="pipeline">The pipeline to be executed by this step.</param>
    /// <param name="serviceProvider">The service provider used to resolve dependencies.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pipeline"/> or <paramref name="serviceProvider"/> is <see langword="null"/>.
    /// </exception>
    public RunPipelineStep(IPipeline pipeline, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    /// <summary>
    /// Executes the core logic of the pipeline step asynchronously.
    /// </summary>
    /// <remarks>
    /// This method is responsible for running the associated <see cref="IPipeline"/> and handling its lifecycle.
    /// It logs the start and completion of the pipeline execution, and captures any exceptions that occur during execution.
    /// </remarks>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="Exception">The pipeline execution encounters an error.</exception>
    protected override async Task RunCoreAsync(CancellationToken token)
    {
        Logger?.LogTrace("Running {Pipeline}...", _pipeline);
        try
        {
            await _pipeline.RunAsync(token).ConfigureAwait(false);
            Logger?.LogTrace("Finished {Pipeline}", _pipeline);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Pipeline {Pipeline} finished with exception: {Message}", _pipeline, e.Message);
            throw;
        }
    }

    /// <inheritdoc />
    protected override void DisposeResources()
    {
        base.DisposeResources();
        _pipeline.Dispose();
    }
}