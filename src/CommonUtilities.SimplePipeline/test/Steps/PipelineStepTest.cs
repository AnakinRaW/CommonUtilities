using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class PipelineStepTest : PipelineStepTestSuite
{
    protected override bool StepRespectsCancellationToken => true;
    protected override bool StepAddsExceptionsToErrorProperty => true;

    protected override Type GetExpectedExceptionType(Exception thrownException)
    {
        return thrownException.GetType();
    }

    protected override PipelineStep CreateStep()
    {
        return new TestStep(null, ServiceProvider);
    }

    protected override PipelineStep CreateStepWithAction(Func<CancellationToken, Task> action)
    {
        return new TestStep(action, ServiceProvider);
    }

    #region Ctor

    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestStep(null, null!));
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_IsTypeName()
    {
        var step = new TestStep(null, ServiceProvider);
        Assert.Equal(step.GetType().Name, step.ToString());
    }

    #endregion

    #region Error

    [Theory]
    [MemberData(nameof(StepsThatThrowError_TestData))]
    public async Task Error_Cancel_PropertyIsCorrectlySet(PipelineStep step, bool shouldContainError, bool isCancel)
    {
        try
        {
            await step.RunAsync(TestContext.Current.CancellationToken);
        }
        catch
        {
            // Ignore
        }
        
        Assert.Equal(shouldContainError, step.Error is not null);
        
        Assert.Equal(isCancel, step.IsCancelled);
    }

    #endregion
    
    public static IEnumerable<object[]> StepsThatThrowError_TestData()
    {
        foreach (var step in StepsWhichEvaluateToNullErrorProperty())
            yield return [step.step, false, step.cancel];
        foreach (var step in CancelledStepsWithErrorProperty())
            yield return [step, true, true];
        foreach (var step in FailedSteps())
            yield return [step, true, false];
    }

    private static IEnumerable<(ErrorStep step, bool cancel)> StepsWhichEvaluateToNullErrorProperty()
    {
        yield return (new ErrorStep(null), false);
        yield return (new ErrorStep(new StopRunnerException()), false);
        yield return (new ErrorStep(new OperationCanceledException()), true);
        yield return (new ErrorStep(new TaskCanceledException()), true);
        yield return (new ErrorStep(new AggregateException(new OperationCanceledException())), true);
        yield return (new ErrorStep(new AggregateException(new TaskCanceledException())), true);
        yield return (new ErrorStep(new AggregateException(new AggregateException(new OperationCanceledException()))), true); 
        yield return (new ErrorStep(new AggregateException(new Exception(), new OperationCanceledException())), true);
    }

    private static IEnumerable<ErrorStep> CancelledStepsWithErrorProperty()
    {
        yield return new ErrorStep(new OperationCanceledException("Cancel", new Exception("Test")));
        yield return new ErrorStep(new AggregateException(new OperationCanceledException("Cancel", new Exception("Test"))));
        yield return new ErrorStep(new AggregateException(new AggregateException(new OperationCanceledException("Cancel", new Exception("Test")))));
    }

    private static IEnumerable<ErrorStep> FailedSteps()
    {
        yield return new ErrorStep(new Exception());
        yield return new ErrorStep(new AggregateException(new ArgumentException()));
    }
}