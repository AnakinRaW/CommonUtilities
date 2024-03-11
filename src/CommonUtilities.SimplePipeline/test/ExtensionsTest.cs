using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class ExtensionsTest
{
    [Fact]
    public void Test_IsExceptionType()
    {
        var e = new InvalidOperationException();

        Assert.True(e.IsExceptionType<Exception>());
        Assert.False(e.IsExceptionType<IOException>());

        var io = new IOException();
        Assert.True(io.IsExceptionType<Exception>());
        Assert.True(io.IsExceptionType<IOException>());

        var a = new AggregateException(new List<Exception> { e, io });
        Assert.True(a.IsExceptionType<InvalidOperationException>());
        Assert.True(a.IsExceptionType<IOException>());
    }
}