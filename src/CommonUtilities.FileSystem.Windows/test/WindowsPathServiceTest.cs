using System;
using System.IO;
using System.Security.AccessControl;
using Sklavenwalker.CommonUtilities.FileSystem.Windows;
using Xunit;

namespace Commonutilities.FileSystem.Windows.Test;

public class WindowsPathServiceTest
{
    private readonly WindowsPathService? _service;

    public WindowsPathServiceTest()
    {
#if NET
            if (!OperatingSystem.IsWindows())
                return;
#endif
        _service = new WindowsPathService();
    }

    [Theory]
    [InlineData(".....")]
    [InlineData("con")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM0")]
    [InlineData("COM1")]
    [InlineData("COM2")]
    [InlineData("COM3")]
    [InlineData("COM4")]
    [InlineData("COM5")]
    [InlineData("COM6")]
    [InlineData("COM7")]
    [InlineData("COM8")]
    [InlineData("COM9")]
    [InlineData("lpt0")]
    [InlineData("lpt1")]
    [InlineData("lpt2")]
    [InlineData("lpt3")]
    [InlineData("lpt4")]
    [InlineData("lpt5")]
    [InlineData("lpt6")]
    [InlineData("lpt7")]
    [InlineData("lpt8")]
    [InlineData("lpt9")]
    [InlineData("#123")]
    [InlineData("\\file")]
    public void TestInvalidFileName(string input)
    {
#if NET
            if (!OperatingSystem.IsWindows())
                return;
#endif
        Assert.False(_service!.IsValidFileName(input));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData(".abc")]
    [InlineData(".con")]
    public void TestValidFileName(string input)
    {
#if NET
            if (!OperatingSystem.IsWindows())
                return;
#endif
        Assert.True(_service!.IsValidFileName(input));
    }

    [Theory]
    [InlineData("C:\\")]
    [InlineData("C:\\Data")]
    [InlineData("C:\\Data\\")]
    [InlineData("\\\\network\\")]
    [InlineData("\\\\network\\path")]
    public void TestValidAbsolutePath(string input)
    {
#if NET
            if (!OperatingSystem.IsWindows())
                return;
#endif
        Assert.True(_service!.IsValidAbsolutePath(input));
        Assert.True(_service.IsValidPath(input));
    }

        
    [Theory]
    [InlineData("lpt22")]
    [InlineData(".abc")]
    public void TestValidPath(string input)
    {
#if NET
            if (!OperatingSystem.IsWindows())
                return;
#endif
        Assert.Throws<InvalidOperationException>(() => _service!.IsValidAbsolutePath(input));
    }

    [Theory]
    [InlineData("C:\\con\\")]
    [InlineData("C:\\con\\sub")]
    [InlineData("C:\\AUX\\")]
    [InlineData("C:\\PRN\\")]
    [InlineData("C:\\NUL\\")]
    [InlineData("C:\\COM0\\")]
    [InlineData("C:\\COM1\\")]
    [InlineData("C:\\COM2\\")]
    [InlineData("C:\\COM3\\")]
    [InlineData("C:\\COM4\\")]
    [InlineData("C:\\COM5\\")]
    [InlineData("C:\\COM6\\")]
    [InlineData("C:\\COM7\\")]
    [InlineData("C:\\COM8\\")]
    [InlineData("C:\\COM9\\")]
    [InlineData("C:\\lpt0\\")]
    [InlineData("C:\\lpt1\\")]
    [InlineData("C:\\lpt2\\")]
    [InlineData("C:\\lpt3\\")]
    [InlineData("C:\\lpt4\\")]
    [InlineData("C:\\lpt5\\")]
    [InlineData("C:\\lpt6\\")]
    [InlineData("C:\\lpt7\\")]
    [InlineData("C:\\lpt8\\")]
    [InlineData("C:\\lpt9\\")]
    public void TestInvalidPath(string input)
    {
#if NET
            if (!OperatingSystem.IsWindows())
                return;
#endif
        Assert.False(_service!.IsValidAbsolutePath(input));
        Assert.False(_service.IsValidPath(input));
    }

    [Theory]
    [InlineData("C:\\Test", DriveType.Fixed)]
    //[InlineData("E:\\Test", DriveType.CDRom)]
    [InlineData("X:\\Test", DriveType.NoRootDirectory)]
    public void TestDriveType(string input, DriveType type)
    {
#if NET
            if (!OperatingSystem.IsWindows())
                return;
#endif
        Assert.Equal(type, _service!.GetDriveType(input));
    }

    [Theory]
    [InlineData("Test")]
    public void TestDriveTypeThrows(string input)
    {
#if NET
            if (!OperatingSystem.IsWindows())
                return;
#endif
        Assert.Throws<InvalidOperationException>(() => _service!.GetDriveType(input));
    }

    [Theory]
    [InlineData("C:\\", true)]
    [InlineData("C:\\System Volume Information", false)]
    public void TestAccessRights(string input, bool expected)
    {
#if NET
            if (!OperatingSystem.IsWindows())
                return;
#endif
        Assert.Equal(expected, _service!.UserHasDirectoryAccessRights(input, FileSystemRights.Read));
    }
}