using System;
using System.Threading;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class StepFailureExceptionTests : CommonTestBase
{
    [Fact]
    public void Ctor_WithNullFailedSteps_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new StepFailureException(null!));
    }

    [Fact]
    public void Message_WithNoFailedSteps_ReturnsEmptyString()
    {
        var ex = new StepFailureException([]);
        Assert.Equal(string.Empty, ex.Message);
    }

    [Fact]
    public void Message_WithOneFailedStep_ReturnsErrorMessage()
    {
        var step = new TestStep(_ => throw new Exception("TestError"), ServiceProvider);

        try
        {
            step.Run(CancellationToken.None);
        }
        catch
        {
            // Ignore
        }

        var ex = new StepFailureException([step]);

        Assert.Equal("Step 'TestStep' failed with error: TestError", ex.Message);
    }

    [Fact]
    public void Message_WithMultipleFailedSteps_ReturnsErrorMessage()
    {
        var step1 = new TestStep(_ => throw new Exception("TestError1"), ServiceProvider);
        var step2 = new TestStep(_ => throw new Exception("TestError2"), ServiceProvider);

        try
        {
            step1.Run(CancellationToken.None);
        }
        catch
        {
            // Ignore
        }
        try
        {
            step2.Run(CancellationToken.None);
        }
        catch
        {
            // Ignore
        }

        var ex = new StepFailureException([step1, step2]);

        Assert.Equal("Step 'TestStep' failed with error: TestError1;Step 'TestStep' failed with error: TestError2", ex.Message);
    }
}