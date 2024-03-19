using System;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class PathAreEqualTest
{
    private readonly MockFileSystem _fileSystem = new();

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData("a", "A", true)]
    [InlineData("a/", "a/", true)]
    [InlineData("a/", "A/", true)]
    [InlineData("a/", "A\\", true)]
    [InlineData("a/b", "A\\b", true)]
    [InlineData("/a", "\\a", true)]
    [InlineData("C:\\a", "c:/A", true)]
    [InlineData("a", "b", false)]
    [InlineData("/a", "a\\", false)]
    [InlineData("a/", "a", false)]
    [InlineData("a/b", "a/c", false)]
    public void Test_AreEqual_Windows(string pathA, string pathB, bool areEqual)
    {
        Assert.Equal(areEqual, _fileSystem.Path.AreEqual(pathA, pathB));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData("a", "a", true)]
    [InlineData("a/", "a/", true)]
    [InlineData("a/b", "a/b", true)]
    [InlineData("/a/b", "/a/b", true)]
    [InlineData("a", "A", false)]
    [InlineData("a/", "a", false)]
    [InlineData("a/b", "a/B", false)]
    [InlineData("a/", "a\\", false)]
    public void Test_AreEqual_Linux(string pathA, string pathB, bool areEqual)
    {
        Assert.Equal(areEqual, _fileSystem.Path.AreEqual(pathA, pathB));
    }

    [Theory]
    [InlineData(null, "a")]
    [InlineData(null, null)]
    [InlineData("a", null)]
    [InlineData("", "a")]
    [InlineData("", "")]
    [InlineData("a", "")]
    public void Test_AreEqual_ThrowsAnyArgumentException(string pathA, string pathB)
    {
        Assert.ThrowsAny<ArgumentException>(() => _fileSystem.Path.AreEqual(pathA, pathB));
    }
}