using System;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class StepErrorEventArgsTest : CommonTestBase
{
    [Fact]
    public void Cancel()
    {
        var e = new Exception("Tet");
        var step = new TestStep(_ => { }, ServiceProvider);
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