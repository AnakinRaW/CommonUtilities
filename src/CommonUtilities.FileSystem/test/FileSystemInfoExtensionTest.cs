using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;


namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class FileSystemInfoExtensionTest
{
    private readonly MockFileSystem _fileSystem = new();

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData("C:\\")]
    [InlineData("C:\\test")]
    [InlineData("C:\\test.txt")]
    [InlineData("test.txt")]
    [InlineData("/")]
    public void Test_GetDriveSize_Windows(string path)
    {
        _fileSystem.WithDrive("C:", c => c.SetTotalSize(1234));
        var fsi = _fileSystem.FileInfo.New(path);
        var size = fsi.GetDriveFreeSpace();
        Assert.True(size == 1234);
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData("/")]
    [InlineData("/a")]
    [InlineData("a/")]
    [InlineData("a")]
    public void Test_GetDriveSize_Linux(string path)
    {
        _fileSystem.WithDrive("/", c => c.SetTotalSize(1234));
        var fsi = _fileSystem.FileInfo.New(path);
        var size = fsi.GetDriveFreeSpace();
        Assert.True(size == 1234);
    }

}