using System;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class HasTrailingPathSeparatorTest
{
    // Using the actual file system here since we are not modifying it.
    // Also, we want to assure that everything works on the real system,
    // not that an arbitrary test implementation works.
    private readonly IFileSystem _fileSystem = new System.IO.Abstractions.FileSystem();
    
    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Test_HasTrailingPathSeparator(string? input, bool expected)
    {
        if (input is null)
            Assert.Throws<ArgumentNullException>(() => _fileSystem.Path.HasTrailingDirectorySeparator(input));
        else
            Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input));
        Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input.AsSpan()));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData("/", true)]
    [InlineData("\\", true)]
    [InlineData("a", false)]
    [InlineData("a/", true)]
    [InlineData("a\\", true)]
    [InlineData("a\\b", false)]
    [InlineData("a/b", false)]
    [InlineData("a/b\\", true)]
    [InlineData("a\\b/", true)]
    public void Test_HasTrailingPathSeparator_Windows(string input, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input));
        Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input.AsSpan()));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData("/", true)]
    [InlineData("\\", false)]
    [InlineData("a", false)]
    [InlineData("a/", true)]
    [InlineData("a\\", false)]
    [InlineData("a\\b", false)]
    [InlineData("a/b", false)]
    [InlineData("a/b\\", false)]
    [InlineData("a\\b/", true)]
    [InlineData("a\\b\\/", true)]
    [InlineData("a\\b/\\", false)]
    public void Test_HasTrailingPathSeparator_Linux(string input, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input));
        Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input.AsSpan()));
    }
}