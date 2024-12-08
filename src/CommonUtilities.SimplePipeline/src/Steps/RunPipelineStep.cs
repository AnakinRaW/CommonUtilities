using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// A step that executes a pipeline and waits for the pipeline to end.
/// </summary>
/// <param name="pipeline">The pipeline to execute.</param>
/// <param name="serviceProvider">The service provider</param>
public class RunPipelineStep(IPipeline pipeline, IServiceProvider serviceProvider) : SynchronizedStep(serviceProvider)
{
    private readonly IPipeline _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

    /// <inheritdoc />
    protected override void RunSynchronized(CancellationToken token)
    {
        Logger?.LogInformation($"Running {_pipeline}...");
        try
        {
            _pipeline.RunAsync(token).Wait();
            Logger?.LogInformation($"Finished {_pipeline}");
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
    protected override void DisposeManagedResources()
    {
        base.DisposeManagedResources();
        _pipeline.Dispose();
    }
}