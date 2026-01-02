using AnakinRaW.CommonUtilities.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing.Extensions;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class ExtensionsTest : TestBaseWithServiceProvider
{
    [Fact]
    public void IsExceptionType()
    {
        var i = new InvalidOperationException();

        Assert.True(i.IsExceptionType<Exception>());
        Assert.False(i.IsExceptionType<IOException>());

        var io = new IOException();
        Assert.True(io.IsExceptionType<Exception>());
        Assert.True(io.IsExceptionType<IOException>());

        var a = new AggregateException(new List<Exception> { i, io });
        Assert.True(a.IsExceptionType<AggregateException>());
        Assert.True(a.IsExceptionType<InvalidOperationException>());
        Assert.True(a.IsExceptionType<IOException>());

        var n = new NullReferenceException();
        var da = new AggregateException(a, n);
        Assert.True(da.IsExceptionType<AggregateException>());
        Assert.True(da.IsExceptionType<InvalidOperationException>());
        Assert.True(da.IsExceptionType<IOException>());
        Assert.True(da.IsExceptionType<NullReferenceException>());
    }

    [Fact]
    public void ThrowStepFailureExceptionForFailedSteps_DoesNotThrowIfEmpty()
    {
        IEnumerable<IStep> steps = [];
        var exception = Record.Exception(() => steps.ThrowStepFailureExceptionForFailedSteps());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowStepFailureExceptionForFailedSteps_DoesNotThrowIfNoStepFailed()
    {
        var step1 = new ErrorStep { Error = null };
        var step2 = new ErrorStep { Error = new OperationCanceledException() };
        var step3 = new ErrorStep { Error = new AggregateException(new OperationCanceledException()) };
        var step4 = new ErrorStep { Error = new AggregateException(new Exception(), new OperationCanceledException()) };

        IEnumerable<IStep> steps = [step1, step2, step3, step4];
        var exception = Record.Exception(() => steps.ThrowStepFailureExceptionForFailedSteps());
        Assert.Null(exception);
    }

    private static IEnumerable<ErrorStep> GetNonFailedSteps()
    {
        yield return new ErrorStep { Error = null };
        yield return new ErrorStep { Error = new OperationCanceledException() };
        yield return new ErrorStep { Error = new TaskCanceledException() };
        yield return new ErrorStep { Error = new AggregateException(new OperationCanceledException()) };
        yield return new ErrorStep { Error = new AggregateException(new TaskCanceledException()) };
        yield return new ErrorStep { Error = new AggregateException(new AggregateException(new OperationCanceledException())) };
        yield return new ErrorStep { Error = new AggregateException(new Exception(), new OperationCanceledException()) };
    }

    private static IEnumerable<ErrorStep> GetFailedSteps()
    {
        yield return new ErrorStep { Error = new Exception() };
        yield return new ErrorStep { Error = new AggregateException(new ArgumentException()) };
    }
    
    [Fact]
    public void ThrowStepFailureExceptionForFailedSteps_ThrowsIfOneStepFailed()
    {
        var exception = Assert.Throws<StepFailureException>(() => 
            GetNonFailedSteps().Append(GetFailedSteps().First())
            .ThrowStepFailureExceptionForFailedSteps());
        Assert.Single(exception.FailedSteps);
    }

    [Fact]
    public void ThrowStepFailureExceptionForFailedSteps_ThrowsIfAllStepsFailed()
    {
        var failed = GetFailedSteps().ToList();
        var exception = Assert.Throws<StepFailureException>(() => failed.ThrowStepFailureExceptionForFailedSteps());
        Assert.EqualUnordered(failed, exception.FailedSteps.ToList());
    }

    [Fact]
    public void ThrowStepFailureExceptionForFailedSteps_ThrowsIfManyStepsFailed()
    {
        var failed = GetFailedSteps().ToList();
        var exception = Assert.Throws<StepFailureException>(() => failed.Concat(GetNonFailedSteps()).ThrowStepFailureExceptionForFailedSteps());
        Assert.All(exception.FailedSteps, step =>
        {
            Assert.Contains(step, failed);
        });
    }

    private class ErrorStep : IStep
    {
        public Exception? Error { get; init; }

        public void Dispose()
        {
        }

        public Task RunAsync(CancellationToken token) => throw new NotImplementedException();

        public TaskAwaiter GetAwaiter() => throw new NotImplementedException();
    }
}