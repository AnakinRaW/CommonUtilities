using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Progress;

public class ProgressEventArgsTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public void Ctor_WithDefaultInfo(string? text)
    {
        var value = new Random().NextDouble();
        var args = new ProgressEventArgs<TestInfoStruct>(value, text);

        Assert.Equal(0, args.ProgressInfo.Progress);
        Assert.Equal(value, args.Progress);
        Assert.Equal(text, args.ProgressText);
    }

    [Fact]
    public void Ctor_WithExplicitInfo()
    {
        var value = new Random().NextDouble();
        var args = new ProgressEventArgs<TestInfoStruct>(0.5, "abc", new TestInfoStruct
        {
            Progress = value
        });

        Assert.Equal(value, args.ProgressInfo.Progress);
        Assert.Equal(0.5, args.Progress);
        Assert.Equal("abc", args.ProgressText);
    }
}