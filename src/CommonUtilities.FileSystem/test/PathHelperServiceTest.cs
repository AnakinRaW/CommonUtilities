using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class PathExtensionsTest
{
    private readonly IFileSystem _fileSystem = new MockFileSystem();

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    public void TestIsChild_Windows(string basePath, string candidate, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.IsChildOf(basePath, candidate));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    public void TestIsChild_Linux(string basePath, string candidate, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.IsChildOf(basePath, candidate));
    }

    [Theory]
    public void TestEnsureTrailing(string path, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.EnsureTrailingSeparator(path));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    public void TestIsAbsolute_Windows(string path, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.IsAbsolute(path));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    public void TestIsAbsolute_Linux(string path, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.IsAbsolute(path));
    }
}