using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class IsChildOfTest
{
    private readonly IFileSystem _fileSystem = new MockFileSystem();

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData("C:/", "C:/", false)]
    [InlineData("C:/a/b", "C:/a", false)]
    [InlineData("C:/a1", "C:/a/b", false)]
    [InlineData("C:/a", "C:/a1/b", false)]
    [InlineData("C:/a", "a", false)]
    [InlineData("C:/a", "C:a", false)]
    [InlineData("C:/", "D:", false)]
    [InlineData("C:/", "D:a", false)]
    [InlineData("C:/", "C:/a", true)]
    [InlineData("C:\\", "C:/a", true)]
    [InlineData("C:\\", "C:\\a", true)]
    [InlineData("C:\\", "C:\\a\\", true)]
    [InlineData("C:/a", "C:/a/b", true)]
    [InlineData("C:/a", "C:/A/b", true)]
    [InlineData("C:/", "C:/a/b", true)]
    [InlineData("C:/", "a", true)]
    [InlineData("C:/current", "a", true)]
    [InlineData("C:/", "C:a", true)]
    [InlineData("C:", "C:a", true)]
    [InlineData("C:", "a", true)]
    public void TestIsChild_Windows(string basePath, string candidate, bool expected)
    {
        _fileSystem.Initialize().WithSubdirectory("C:\\current");
        _fileSystem.Directory.SetCurrentDirectory("C:\\current");
        Assert.Equal(expected, _fileSystem.Path.IsChildOf(basePath, candidate));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData("/", "/", false)]
    [InlineData("/a", "/A/b", false)]
    [InlineData("/a", "a", false)]
    [InlineData("/a", "/a/b", true)]
    [InlineData("/", "a", true)]
    [InlineData("/current", "a", true)]
    public void TestIsChild_Linux(string basePath, string candidate, bool expected)
    {
        _fileSystem.Initialize().WithSubdirectory("/current");
        _fileSystem.Directory.SetCurrentDirectory("/current");
        Assert.Equal(expected, _fileSystem.Path.IsChildOf(basePath, candidate));
    }
}