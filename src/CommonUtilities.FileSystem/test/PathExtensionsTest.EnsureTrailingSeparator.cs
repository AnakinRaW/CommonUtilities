using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class EnsureTrailingSeparatorTest
{
    private readonly IFileSystem _fileSystem = new MockFileSystem();

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData("", "")]
    [InlineData("/", "/")]
    [InlineData("a", "a\\")]
    [InlineData("\\", "\\")]
    [InlineData("a/b", "a/b/")]
    [InlineData("a\\b", "a\\b\\")]
    [InlineData("a\\b/c", "a\\b/c\\")]
    [InlineData("a/b\\", "a/b\\")]
    [InlineData("a\\b/", "a\\b/")]
    public void Test_EnsureTrailingSeparator_Windows(string input, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.EnsureTrailingSeparator(input));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData("", "")]
    [InlineData("/", "/")]
    [InlineData("\\", "\\/")]
    [InlineData("a", "a/")]
    [InlineData("a/b", "a/b/")]
    [InlineData("a/b\\", "a/b\\/")]
    [InlineData("a\\b\\", "a\\b\\/")]
    public void Test_EnsureTrailingSeparator_Linux(string input, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.EnsureTrailingSeparator(input));
    }
}