using System;
using System.Threading;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class PipelineStepTest : CommonTestBase
{
    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestStep(_ => { }, null!));
    }

    [Fact]
    public void Disposed()
    {
        var step = new TestStep(_ => { }, ServiceProvider);

        step.Dispose();
        Assert.True(step.IsDisposed);
    }

    [Fact]
    public void Run()
    {
        var ran = false;
        var step = new TestStep(_ => { ran = true; }, ServiceProvider);

        step.Run(CancellationToken.None);

        Assert.True(ran);
    }

    [Fact]
    public void Run_ThrowsException()
    {
        var expectedError = new Exception();

        var step = new TestStep(_ => throw expectedError, ServiceProvider);

        Assert.Throws<Exception>(() => step.Run(CancellationToken.None));
        Assert.Same(expectedError, step.Error);
    }

    [Fact]
    public void Run_WithCancellation_ThrowsOperationCanceledException()
    {
        var step = new TestStep(ct =>
        {
            ct.ThrowIfCancellationRequested();
        }, ServiceProvider);

        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        Assert.Throws<OperationCanceledException>(() => step.Run(cts.Token));
        Assert.Null(step.Error);
    }

    [Fact]
    public void Run_StopRunnerException_IsNotAddedToErrors()
    {
        var step = new TestStep(_ => throw new StopRunnerException(), ServiceProvider);

        Assert.Throws<StopRunnerException>(() => step.Run(CancellationToken.None));
        Assert.Null(step.Error);
    }

    [Fact]
    public void Run_AggregateException()
    {
        var expected = new AggregateException(new Exception("Test"));
        var step = new TestStep(_ => throw expected, ServiceProvider);

        Assert.Throws<AggregateException>(() => step.Run(CancellationToken.None));
        Assert.Same(expected, step.Error);
    }

    [Fact]
    public void Run_AggregateException_OriginatedFromOperationCancelled()
    {
        var expected = new Exception("Test");
        var step = new TestStep(_ => throw new AggregateException(new OperationCanceledException(null, expected)), ServiceProvider);

        Assert.Throws<AggregateException>(() => step.Run(CancellationToken.None));
        Assert.Same(expected, step.Error);
    }

    [Fact]
    public void Run_AggregateException_OriginatedFromOperationCancelled_NoInnerException()
    {
        var step = new TestStep(_ => throw new AggregateException(new OperationCanceledException()), ServiceProvider);

        Assert.Throws<AggregateException>(() => step.Run(CancellationToken.None));
        Assert.Null(step.Error);
    }

    [Fact]
    public void ToString_IsTypeName()
    {
        var step = new TestStep(_ => { }, ServiceProvider);
        Assert.Equal(step.GetType().Name, step.ToString());
    }
}