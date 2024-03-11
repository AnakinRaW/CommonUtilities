using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class StepErrorEventArgsTest
{
    [Fact]
    public void Test_Cancel()
    {
        var step = new Mock<IStep>();
        var args = new StepErrorEventArgs(step.Object);
        Assert.False(args.Cancel);
        args.Cancel = true;
        Assert.True(args.Cancel);
        args.Cancel = false;
        Assert.True(args.Cancel);
    }
}