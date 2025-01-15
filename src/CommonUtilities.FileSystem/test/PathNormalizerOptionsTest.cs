using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class PathNormalizerOptionsTest
{
    [Fact]
    public void Default()
    {
        var options = default(PathNormalizeOptions);
        Assert.False(options.TreatBackslashAsSeparator);
        Assert.False(options.UnifyDirectorySeparators);
        Assert.Equal(DirectorySeparatorKind.System, options.UnifySeparatorKind);
        Assert.Equal(TrailingDirectorySeparatorBehavior.None, options.TrailingDirectorySeparatorBehavior);
        Assert.Equal(UnifyCasingKind.None, options.UnifyCase);
    }

    [Fact]
    public void UnifySeparators()
    {
        var options = PathNormalizeOptions.UnifySeparators;
        Assert.False(options.TreatBackslashAsSeparator);
        Assert.True(options.UnifyDirectorySeparators);
        Assert.Equal(DirectorySeparatorKind.System, options.UnifySeparatorKind);
        Assert.Equal(TrailingDirectorySeparatorBehavior.None, options.TrailingDirectorySeparatorBehavior);
        Assert.Equal(UnifyCasingKind.None, options.UnifyCase);
    }

    [Fact]
    public void UnifyUpper()
    {
        var options = PathNormalizeOptions.UnifyUpper;
        Assert.False(options.TreatBackslashAsSeparator);
        Assert.False(options.UnifyDirectorySeparators);
        Assert.Equal(DirectorySeparatorKind.System, options.UnifySeparatorKind);
        Assert.Equal(TrailingDirectorySeparatorBehavior.None, options.TrailingDirectorySeparatorBehavior);
        Assert.Equal(UnifyCasingKind.UpperCase, options.UnifyCase);
    }

    [Fact]
    public void AlwaysUnifyUpper()
    {
        var options = PathNormalizeOptions.AlwaysUnifyUpper;
        Assert.False(options.TreatBackslashAsSeparator);
        Assert.False(options.UnifyDirectorySeparators);
        Assert.Equal(DirectorySeparatorKind.System, options.UnifySeparatorKind);
        Assert.Equal(TrailingDirectorySeparatorBehavior.None, options.TrailingDirectorySeparatorBehavior);
        Assert.Equal(UnifyCasingKind.UpperCaseForce, options.UnifyCase);
    }

    [Fact]
    public void TrimTrailingSeparators()
    {
        var options = PathNormalizeOptions.TrimTrailingSeparators;
        Assert.False(options.TreatBackslashAsSeparator);
        Assert.False(options.UnifyDirectorySeparators);
        Assert.Equal(DirectorySeparatorKind.System, options.UnifySeparatorKind);
        Assert.Equal(TrailingDirectorySeparatorBehavior.Trim, options.TrailingDirectorySeparatorBehavior);
        Assert.Equal(UnifyCasingKind.None, options.UnifyCase);
    }

    [Fact]
    public void EnsureTrailingSeparator()
    {
        var options = PathNormalizeOptions.EnsureTrailingSeparator;
        Assert.False(options.TreatBackslashAsSeparator);
        Assert.False(options.UnifyDirectorySeparators);
        Assert.Equal(DirectorySeparatorKind.System, options.UnifySeparatorKind);
        Assert.Equal(TrailingDirectorySeparatorBehavior.Ensure, options.TrailingDirectorySeparatorBehavior);
        Assert.Equal(UnifyCasingKind.None, options.UnifyCase);
    }
}