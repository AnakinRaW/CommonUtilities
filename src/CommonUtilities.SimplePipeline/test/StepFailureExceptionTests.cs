using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class StepFailureExceptionTests : TestBaseWithServiceProvider
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
    public async Task Message_WithOneFailedStep_ReturnsErrorMessage()
    {
        var step = new TestStep(_ => throw new Exception("TestError"), ServiceProvider);

        try
        {
            await step.RunAsync(CancellationToken.None);
        }
        catch
        {
            // Ignore
        }

        var ex = new StepFailureException([step]);

        Assert.Equal("Step 'TestStep' failed with error: TestError", ex.Message);
    }

    [Fact]
    public async Task Message_WithMultipleFailedAndNonFailedSteps_ReturnsErrorMessage()
    {
        var step1 = new TestStep(_ => throw new Exception("TestError1"), ServiceProvider);
        var step2 = new TestStep(_ => throw new Exception("TestError2"), ServiceProvider);
        var step3 = new TestStep(_ => Task.CompletedTask, ServiceProvider);

        try
        {
            await step1.RunAsync(CancellationToken.None);
        }
        catch
        {
            // Ignore
        }
        try
        {
            await step2.RunAsync(CancellationToken.None);
        }
        catch
        {
            // Ignore
        }

        await step3.RunAsync(CancellationToken.None);

        var ex = new StepFailureException([step1, step2, step3]);

        Assert.Equal(
            "Step 'TestStep' failed with error: TestError1;Step 'TestStep' failed with error: TestError2;Step 'TestStep' failed with error: ", 
            ex.Message);
    }
}