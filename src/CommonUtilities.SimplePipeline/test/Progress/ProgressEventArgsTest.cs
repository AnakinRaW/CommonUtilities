using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Progress;

public class ProgressEventArgsTest
{
    private struct TestInfo
    {
        public int Value;
    }

    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ProgressEventArgs<TestInfo>(null!, 0.5, new ProgressType
        {
            DisplayName = "name",
            Id = "id"
        }));
        Assert.Throws<ArgumentException>(() => new ProgressEventArgs<TestInfo>(string.Empty, 0.5, new ProgressType
        {
            DisplayName = "name",
            Id = "id"
        }));
    }

    [Fact]
    public void Ctor_WithDefaultInfo()
    {
        var args = new ProgressEventArgs<TestInfo>("abc", 0.5, new ProgressType
        {
            DisplayName = "name", Id = "id"
        });

        Assert.Equal(0, args.ProgressInfo.Value);
        Assert.Equal(0.5, args.Progress);
        Assert.Equal("abc", args.ProgressText);
        Assert.Equal(new ProgressType { Id = "id", DisplayName = "name" }, args.Type);
    }

    [Fact]
    public void Ctor_WithExplicitInfo()
    {
        var args = new ProgressEventArgs<TestInfo>("abc", 0.5, new ProgressType
        {
            DisplayName = "name",
            Id = "id"
        }, new TestInfo{Value = 1});

        Assert.Equal(1, args.ProgressInfo.Value);
        Assert.Equal(0.5, args.Progress);
        Assert.Equal("abc", args.ProgressText);
        Assert.Equal(new ProgressType { Id = "id", DisplayName = "name" }, args.Type);
    }
}