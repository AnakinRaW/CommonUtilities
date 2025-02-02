using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Progress;

public class ProgressTypeTest
{
    [Fact]
    public void InvalidValues_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ProgressType
        {
            DisplayName = null!,
            Id = "id"
        });
        Assert.Throws<ArgumentNullException>(() => new ProgressType
        {
            DisplayName = "name",
            Id = null!
        });
        Assert.Throws<ArgumentException>(() => new ProgressType
        {
            DisplayName = "",
            Id = "id"
        });
        Assert.Throws<ArgumentException>(() => new ProgressType
        {
            DisplayName = "name",
            Id = ""
        });
    }

    [Fact]
    public void EqualsGetHashCode()
    {
        var pt = new ProgressType
        {
            DisplayName = "123",
            Id = "test"
        };

        var equal = new ProgressType
        {
            DisplayName = "otherName",
            Id = "test"
        };

        var other = new ProgressType
        {
            DisplayName = "other",
            Id = "other"
        };


        Assert.False(pt.Equals(null));

        Assert.True(pt.Equals(pt));
        Assert.True(pt.Equals((object)pt));
        Assert.True(pt == pt);
        Assert.False(pt != pt);
        Assert.True(pt.Equals(equal));
        Assert.True(pt == equal);
        Assert.False(pt != equal);
        Assert.True(pt.Equals((object)equal));

        Assert.Equal(pt.GetHashCode(), pt.GetHashCode());
        Assert.Equal(pt.GetHashCode(), equal.GetHashCode());

        Assert.False(pt.Equals(other));
        Assert.False(pt == other);
        Assert.True(pt != other);
        Assert.NotEqual(pt.GetHashCode(), other.GetHashCode());
    }
}