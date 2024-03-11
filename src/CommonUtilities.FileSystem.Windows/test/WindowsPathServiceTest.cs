using System.IO;
using System.Security.AccessControl;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Windows.Test;

public class WindowsPathServiceTest
{
    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData(null, FileSystemRights.Read, true)]
    [InlineData(null, FileSystemRights.Write, true)]
    [InlineData("C:\\", FileSystemRights.Read, true)]
    [InlineData("C:\\System Volume Information", FileSystemRights.Read, false)]
    [InlineData("C:\\System Volume Information", FileSystemRights.Write, false)]
    public void Test_UserHasDirectoryAccessRights(string input, FileSystemRights rights, bool expected)
    {
        var fs = new System.IO.Abstractions.FileSystem();
        input ??= fs.Path.GetTempPath();
        var dir = fs.DirectoryInfo.New(input);
        Assert.Equal(expected, dir.UserHasDirectoryAccessRights(rights));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Test_UserHasDirectoryAccessRights_Throws()
    {
        var fs = new System.IO.Abstractions.FileSystem();
        Assert.Throws<DirectoryNotFoundException>(() => fs.DirectoryInfo.New("C:\\doesNotExists\\").UserHasDirectoryAccessRights(FileSystemRights.Read));
    }
}