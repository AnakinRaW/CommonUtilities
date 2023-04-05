using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class StepFailureExceptionTests
{
    [Fact]
    public void Constructor_WithNullFailedSteps_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new StepFailureException(null!));
    }

    [Fact]
    public void Message_WithNoFailedSteps_ReturnsEmptyString()
    {
        var ex = new StepFailureException(new List<IStep>());

        Assert.Equal(string.Empty, ex.Message);
    }

    [Fact]
    public void Message_WithOneFailedStep_ReturnsErrorMessage()
    {
        var step = new Mock<IStep>();
        step.Setup(s => s.ToString()).Returns("TestStep");
        step.Setup(s => s.Error).Returns(new Exception("TestError"));

        var ex = new StepFailureException(new List<IStep> { step.Object });

        Assert.Equal("Step 'TestStep' failed with error: TestError", ex.Message);
    }

    [Fact]
    public void Message_WithMultipleFailedSteps_ReturnsErrorMessage()
    {
        var step1 = new Mock<IStep>();
        step1.Setup(s => s.ToString()).Returns("TestStep1");
        step1.Setup(s => s.Error).Returns(new Exception("TestError1"));

        var step2 = new Mock<IStep>();
        step2.Setup(s => s.ToString()).Returns("TestStep2");
        step2.Setup(s => s.Error).Returns(new Exception("TestError2"));

        var ex = new StepFailureException(new List<IStep> { step1.Object, step2.Object });

        Assert.Equal("Step 'TestStep1' failed with error: TestError1;Step 'TestStep2' failed with error: TestError2", ex.Message);
    }
}