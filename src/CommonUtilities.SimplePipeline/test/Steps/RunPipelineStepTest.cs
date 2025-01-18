using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class RunPipelineStepTest : CommonTestBase
{
    private class DelegatePipeline(Func<CancellationToken, Task> action, IServiceProvider serviceProvider) : Pipeline(serviceProvider)
    {
        protected override Task<bool> PrepareCoreAsync()
        {
            return Task.FromResult(true);
        }

        protected override Task RunCoreAsync(CancellationToken token)
        {
            return action(token);
        }
    }

    [Fact]
    public void Run()
    {
        var ran = false;

        var pipeline = new DelegatePipeline(async ct =>
        {
            await Task.Delay(3000, ct).ConfigureAwait(false);
            ran = true;

        },ServiceProvider);

        var step = new RunPipelineStep(pipeline, ServiceProvider);

        step.Run(CancellationToken.None);
        Assert.True(ran);
    }

    [Fact]
    public void Run_PipelineFails()
    {
        var pipeline = new DelegatePipeline(_ => throw new ApplicationException("test"), ServiceProvider);

        var step = new RunPipelineStep(pipeline, ServiceProvider);

        Assert.Throws<ApplicationException>(() => step.Run(CancellationToken.None));
    }

    [Fact]
    public void Run_Cancel()
    {
        var cts = new CancellationTokenSource();

        var pipeline = new DelegatePipeline(async ct =>
        {
            await Task.Run(() => cts.Cancel(), CancellationToken.None);
            ct.ThrowIfCancellationRequested();
        }, ServiceProvider);

        var step = new RunPipelineStep(pipeline, ServiceProvider);

        Assert.ThrowsAny<OperationCanceledException>(() => step.Run(cts.Token));
    }
}