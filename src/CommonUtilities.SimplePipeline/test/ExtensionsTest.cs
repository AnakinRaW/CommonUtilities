using AnakinRaW.CommonUtilities.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class ExtensionsTest : TestBaseWithServiceProvider
{ 
    public static IEnumerable<object?[]> ExceptionSearchTestCases()
    {
        // Direct match - same type
        {
            var ex = new InvalidOperationException("test");
            yield return [ex, typeof(InvalidOperationException), ex];
        }
        {
            var ex = new IOException("test");
            yield return [ex, typeof(IOException), ex];
        }

        // Direct match - base type
        {
            var ex = new InvalidOperationException("test");
            yield return [ex, typeof(Exception), ex];
        }
        {
            var ex = new IOException("test");
            yield return [ex, typeof(Exception), ex];
        }

        // No match
        yield return [new InvalidOperationException("test"), typeof(IOException), null];
        yield return [new IOException("test"), typeof(InvalidOperationException), null];
        yield return [new InvalidOperationException("test"), typeof(NullReferenceException), null];

        // Simple AggregateException
        {
            var invalidOp = new InvalidOperationException("test");
            var ioEx = new IOException("test");
            var agg = new AggregateException(invalidOp, ioEx);
            yield return [agg, typeof(AggregateException), agg];
            yield return [agg, typeof(InvalidOperationException), invalidOp];
            yield return [agg, typeof(IOException), ioEx];
            yield return [agg, typeof(NullReferenceException), null];
        }

        // Nested AggregateException
        {
            var invalidOp = new InvalidOperationException("test");
            var ioEx = new IOException("test");
            var nullRef = new NullReferenceException("test");
            var innerAgg = new AggregateException(invalidOp, ioEx);
            var outerAgg = new AggregateException(innerAgg, nullRef);
            yield return [outerAgg, typeof(AggregateException), outerAgg];
            yield return [outerAgg, typeof(InvalidOperationException), invalidOp];
            yield return [outerAgg, typeof(IOException), ioEx];
            yield return [outerAgg, typeof(NullReferenceException), nullRef];
            yield return [outerAgg, typeof(ArgumentException), null];
        }

        // A(A(OCE)) - OCE without inner exception
        {
            var oce = new OperationCanceledException("test");
            yield return [new AggregateException(new AggregateException(oce)), typeof(OperationCanceledException), oce];
        }

        // A(A(OCE(E))) - OCE with inner exception
        {
            var oce = new OperationCanceledException("test", new TimeoutException("test"));
            var agg = new AggregateException(new AggregateException(oce));
            yield return [agg, typeof(OperationCanceledException), oce];
            yield return [agg, typeof(TimeoutException), null]; // Not found via InnerExceptions!
        }

        // Deeply nested
        {
            var oce = new OperationCanceledException("test", new TimeoutException("test"));
            var agg = new AggregateException(
                new AggregateException(
                    new AggregateException(
                        new AggregateException(oce))));
            yield return [agg, typeof(OperationCanceledException), oce];
            yield return [agg, typeof(TimeoutException), null]; // Not found via InnerExceptions!
            yield return [agg, typeof(ArgumentException), null];
        }
    }

    public static IEnumerable<object?[]> OceInnerExceptionTestCases()
    {
        // Direct OCE without inner
        yield return [new OperationCanceledException("test"), null];

        // Direct OCE with inner
        {
            var timeout = new TimeoutException("test");
            yield return [new OperationCanceledException("test", timeout), timeout];
        }

        // A(A(OCE)) - no inner
        yield return [new AggregateException(new AggregateException(new OperationCanceledException("test"))), null];

        // A(A(OCE(E))) - with inner
        {
            var timeout = new TimeoutException("test");
            yield return [new AggregateException(new AggregateException(new OperationCanceledException("test", timeout))), timeout];
        }

        // Deeply nested with inner
        {
            var timeout = new TimeoutException("test");
            yield return [new AggregateException(
                new AggregateException(
                    new AggregateException(
                        new AggregateException(
                            new OperationCanceledException("test", timeout))))), timeout];
        }
    }

    [Theory]
    [MemberData(nameof(ExceptionSearchTestCases))]
    public void IsExceptionType_ReturnsExpectedResult(
        Exception source,
        Type searchType,
        Exception? expectedFound)
    {
        var method = typeof(Extensions)
            .GetMethod(nameof(Extensions.IsExceptionType), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(searchType);

        var result = (bool)method.Invoke(null, [source])!;

        Assert.Equal(expectedFound is not null, result);
    }

    [Theory]
    [MemberData(nameof(ExceptionSearchTestCases))]
    public void FindException_ReturnsExpectedResult(Exception source, Type searchType, Exception? expectedFound)
    {
        var method = typeof(Extensions)
            .GetMethod(nameof(Extensions.FindException), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(searchType);

        var result = (Exception?)method.Invoke(null, [source]);

        Assert.Same(expectedFound, result);
    }

    [Theory]
    [MemberData(nameof(OceInnerExceptionTestCases))]
    public void FindException_OperationCanceledException_ReturnsCorrectInnerException(Exception source, Exception? expectedInner)
    {
        var found = source.FindException<OperationCanceledException>();
        Assert.Same(expectedInner, found?.InnerException);
    }

    [Fact]
    public void FindException_ReturnsFirstMatch_WhenMultipleExist()
    {
        var io1 = new IOException("First");
        var io2 = new IOException("Second");
        var aggregate = new AggregateException(io1, io2);
        var found = aggregate.FindException<IOException>();
        Assert.Same(io1, found);
    }
}