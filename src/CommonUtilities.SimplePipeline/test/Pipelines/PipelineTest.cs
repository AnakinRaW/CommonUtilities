using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class PipelineTest : PipelineTestBase
{
    protected override Pipeline CreatePipeline(IList<IStep> steps)
    {
        return new TestPipeline(
            _ => Task.CompletedTask,
            async ct =>
            {
                foreach (var step in steps)
                {
                    await step.RunAsync(ct);
                }
            }, 
            ServiceProvider);
    }

    protected override ITrackingPipeline CreateTrackingPipeline(
        Func<CancellationToken, Task> prepare, 
        Func<CancellationToken, Task> run)
    {
        return new TestPipeline(prepare, run, ServiceProvider);
    }

    private class TestPipeline(
        Func<CancellationToken, Task> prepare, 
        Func<CancellationToken, Task> run, 
        IServiceProvider serviceProvider) : Pipeline(serviceProvider), ITrackingPipeline
    {
        protected override Task PrepareCoreAsync(CancellationToken token)
        {
            return Task.Run(() => prepare(token), CancellationToken.None);
        }

        protected override Task ExecuteAsync(CancellationToken token)
        {
            return Task.Run(() => run(token), CancellationToken.None);
        }
    }
}