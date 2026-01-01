using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// A step that executes a pipeline and waits for the pipeline to end.
/// </summary>
/// <param name="pipeline">The pipeline to execute.</param>
/// <param name="serviceProvider">The service provider</param>
public sealed class RunPipelineStep(IPipeline pipeline, IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
{
    private readonly IPipeline _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

    /// <inheritdoc />
    protected override async Task RunCoreAsync(CancellationToken token)
    {
        Logger?.LogTrace("Running {Pipeline}...", _pipeline);
        try
        {
            await _pipeline.RunAsync(token).ConfigureAwait(false);
            Logger?.LogTrace("Finished {Pipeline}", _pipeline);
        }
        catch (AggregateException e)
        {
            var root = e.InnerExceptions.FirstOrDefault();
            if (root is not null)
                throw root;
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