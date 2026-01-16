using System;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class StepErrorEventArgsTest : TestBaseWithServiceProvider
{
    [Fact]
    public void Cancel()
    {
        var e = new Exception("Tet");
        var step = new TestStep(_ => Task.CompletedTask, ServiceProvider);
        var args = new StepRunnerErrorEventArgs(e, step);

        Assert.Same(step, args.Step);
        Assert.Same(e, args.Exception);

        Assert.False(args.Cancel);
        args.Cancel = true;
        Assert.True(args.Cancel);
        args.Cancel = false;
        Assert.True(args.Cancel);
    }
}