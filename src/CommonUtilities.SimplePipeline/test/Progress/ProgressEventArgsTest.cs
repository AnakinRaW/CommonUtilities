using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Progress;

public class ProgressEventArgsTest
{
    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ProgressEventArgs<TestInfoStruct>(null!, 0.5));
        Assert.Throws<ArgumentException>(() => new ProgressEventArgs<TestInfoStruct>(string.Empty, 0.5));
    }

    [Fact]
    public void Ctor_WithDefaultInfo()
    {
        var args = new ProgressEventArgs<TestInfoStruct>("abc", 0.5);

        Assert.Equal(0, args.ProgressInfo.Progress);
        Assert.Equal(0.5, args.Progress);
        Assert.Equal("abc", args.ProgressText);
    }

    [Fact]
    public void Ctor_WithExplicitInfo()
    {
        var args = new ProgressEventArgs<TestInfoStruct>("abc", 0.5, new TestInfoStruct
        {
            Progress = 1
        });

        Assert.Equal(1, args.ProgressInfo.Progress);
        Assert.Equal(0.5, args.Progress);
        Assert.Equal("abc", args.ProgressText);
    }
}