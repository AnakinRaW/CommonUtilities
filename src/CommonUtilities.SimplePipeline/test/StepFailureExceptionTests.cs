using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
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
    public async Task FailedSteps_IsCopyOfFailedSteps()
    {
        var step1 = await TestStep.CreateFailed(new Exception("TestError 1"), ServiceProvider);
        var step2 = await TestStep.CreateFailed(new Exception("TestError 2"), ServiceProvider);

        List<IStep> steps = [step1];

        var exception = new StepFailureException(steps);
        
        // Adding a step after exception is created
        steps.Add(step2);

        var actualStep = Assert.Single(exception.FailedSteps);
        Assert.Same(step1, actualStep);
    }

    [Fact]
    public void Message_WithNoFailedSteps_ReturnsEmptyString()
    {
        var ex = new StepFailureException([]);
        Assert.Equal("0 Failed Step(s)", ex.Message);
    }

    [Fact]
    public async Task Message_WithOneFailedStep_ReturnsErrorMessage()
    {
        var step = await TestStep.CreateFailed(new Exception("TestError"), ServiceProvider);
        var ex = new StepFailureException([step]);
        Assert.Equal(
            "1 Failed Step(s): " +
            "Step 'TestStep' failed with error: TestError", 
            ex.Message);
    }

    [Fact]
    public async Task Message_WithMultipleFailedAndNonFailedSteps_ReturnsErrorMessage()
    {
        var step1 = await TestStep.CreateFailed(new Exception("TestError1"), ServiceProvider);
        var step2 = await TestStep.CreateFailed(new Exception("TestError2"), ServiceProvider);
        var step3 = await TestStep.CreateFailed(null, ServiceProvider);
        
        var ex = new StepFailureException([step1, step2, step3]);

        Assert.Equal(
            "3 Failed Step(s): " +
            "Step 'TestStep' failed with error: TestError1;" +
            "Step 'TestStep' failed with error: TestError2;" +
            "Step 'TestStep' failed with error: n/a", 
            ex.Message);
    }

    [Fact]
    public async Task Message_CalledMultipleTimes()
    {
        var step1 = await TestStep.CreateFailed(new Exception("TestError1"), ServiceProvider);

        var ex = new StepFailureException([step1]);

        var message1 = ex.Message;
        var message2 = ex.Message;
        
        Assert.Equal(message1, message2);
    }
}