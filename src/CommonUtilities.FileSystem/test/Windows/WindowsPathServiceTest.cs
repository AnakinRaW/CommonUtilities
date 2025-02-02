using System.IO;
using System.Security.AccessControl;
using AnakinRaW.CommonUtilities.FileSystem.Windows;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions;
using Xunit;
#if NET
using System.Runtime.Versioning;
#endif

namespace AnakinRaW.CommonUtilities.FileSystem.Test.Windows;

#if NET
[SupportedOSPlatform("windows")]
#endif
public class WindowsPathServiceTest
{
    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData(null, FileSystemRights.Read, true)]
    [InlineData(null, FileSystemRights.Write, true)]
    [InlineData("C:\\", FileSystemRights.Read, true)]
    [InlineData("C:\\System Volume Information", FileSystemRights.Read, false)]
    [InlineData("C:\\System Volume Information", FileSystemRights.Write, false)]
    public void UserHasDirectoryAccessRights(string? input, FileSystemRights rights, bool expected)
    {
        var fs = new RealFileSystem();
        input ??= fs.Path.GetTempPath();
        var dir = fs.DirectoryInfo.New(input);
        Assert.Equal(expected, dir.UserHasDirectoryAccessRights(rights));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void UserHasDirectoryAccessRights_Throws()
    {
        var fs = new RealFileSystem();
        Assert.Throws<DirectoryNotFoundException>(() => fs.DirectoryInfo.New("C:\\doesNotExists\\").UserHasDirectoryAccessRights(FileSystemRights.Read));
    }
}