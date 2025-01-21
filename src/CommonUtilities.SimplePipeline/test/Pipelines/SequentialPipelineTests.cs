using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

public class SequentialPipelineTests : StepRunnerPipelineTest<SequentialStepRunner>
{
    protected override StepRunnerPipeline<SequentialStepRunner> CreatePipeline(IList<IStep> steps, bool failFast)
    {
        return new TestSequentialPipeline(steps, ServiceProvider, failFast);
    }

    protected override Pipeline CreatePipeline(IList<IStep> steps)
    {
        return CreatePipeline(steps, true);
    }

    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestSequentialPipeline([], null!));
    }

    [Fact]
    public async Task RunAsync_RunsInSequence()
    { 
        var sb = new StringBuilder();

        var s1 = new TestStep(_ => sb.Append('a'), ServiceProvider);
        var s2 = new TestStep(_ => sb.Append('b'), ServiceProvider);

        var pipeline = CreatePipeline([s1, s2], true);
        
        await pipeline.RunAsync();
        Assert.Equal("ab", sb.ToString());

        Assert.False(pipeline.PipelineFailed);
    }

    [Theory]
    [InlineData(true, "")]
    //[InlineData(false, "b")]
    public async Task RunAsync_WithError_FailFastBehavior_Throws(bool failFast, string result)
    {
        var sb = new StringBuilder();

        var s1 = new TestStep(_ => throw new Exception("Test"), ServiceProvider);
        var s2 = new TestStep(_ => sb.Append('b'), ServiceProvider);

        var pipeline = CreatePipeline([s1, s2], failFast);

        var e = await Assert.ThrowsAsync<StepFailureException>(async () => await pipeline.RunAsync());
        Assert.Equal("Step 'TestStep' failed with error: Test", e.Message);
        Assert.Equal(result, sb.ToString());
        Assert.True(pipeline.PipelineFailed);
    }

    private class TestSequentialPipeline(IEnumerable<IStep> steps, IServiceProvider serviceProvider, bool failFast = true)
        : SequentialPipeline(serviceProvider, failFast)
    {
        protected override Task<IList<IStep>> BuildSteps()
        {
            return Task.FromResult<IList<IStep>>(steps.ToList());
        }
    }
}