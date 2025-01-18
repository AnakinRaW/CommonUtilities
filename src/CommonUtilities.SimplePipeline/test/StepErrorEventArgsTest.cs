using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class StepErrorEventArgsTest : CommonTestBase
{
    [Fact]
    public void Test_Cancel()
    {
        var step = new TestStep(_ => { }, ServiceProvider);
        var args = new StepErrorEventArgs(step);

        Assert.Same(step, args.Step);

        Assert.False(args.Cancel);
        args.Cancel = true;
        Assert.True(args.Cancel);
        args.Cancel = false;
        Assert.True(args.Cancel);
    }
}