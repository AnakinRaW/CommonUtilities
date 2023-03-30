using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class TaskErrorEventArgsTest
{
    [Fact]
    public void TestCancelProp()
    {
        var task = new Mock<ITask>();
        var args = new TaskErrorEventArgs(task.Object);
        Assert.False(args.Cancel);
        args.Cancel = true;
        Assert.True(args.Cancel);
        args.Cancel = false;
        Assert.True(args.Cancel);
    }
}