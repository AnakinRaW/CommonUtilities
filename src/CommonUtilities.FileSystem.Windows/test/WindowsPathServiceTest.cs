using System.IO;
using System.Security.AccessControl;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Windows.Test;

public class WindowsPathServiceTest
{
    private readonly MockFileSystem _fileSystem = new();

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData("123")]
    [InlineData("123.txt")]
    [InlineData("123..txt")]
    [InlineData("fileNameWithCase")]
    [InlineData("fileNameWith_underscore")]
    [InlineData("fileNameWith-hyphen")]
    [InlineData(".test")]
    [InlineData("LPT12")]
    [InlineData("COM12")]
    [InlineData("NUL.txt")] // Though it's not recommend by MS, it's actually allowed to use this name in explorer
    [InlineData("nameWithNonASCII_ö")]
    [InlineData("\u0160")]
    public void Test_IsValidFileName_ValidNames(string input)
    {
        Assert.Equal(FileNameValidationResult.Valid, _fileSystem.Path!.IsValidFileName(input));
    }


    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData(null, FileNameValidationResult.NullOrEmpty)]
    [InlineData("", FileNameValidationResult.NullOrEmpty)]
    [InlineData("     ", FileNameValidationResult.LeadingOrTrailingWhiteSpace)]
    [InlineData("\0", FileNameValidationResult.InvalidCharacter)]
    [InlineData("123\0", FileNameValidationResult.InvalidCharacter)]
    [InlineData("123\t", FileNameValidationResult.InvalidCharacter)]
    [InlineData("123\r", FileNameValidationResult.InvalidCharacter)]
    [InlineData("123\n", FileNameValidationResult.InvalidCharacter)]
    [InlineData("nameWithTrailingSpace ", FileNameValidationResult.LeadingOrTrailingWhiteSpace)]
    [InlineData("   nameWithLeadingSpace", FileNameValidationResult.LeadingOrTrailingWhiteSpace)]
    [InlineData("my\\path", FileNameValidationResult.InvalidCharacter)]
    [InlineData("my/path", FileNameValidationResult.InvalidCharacter)]
    [InlineData("illegalChar_<", FileNameValidationResult.InvalidCharacter)]
    [InlineData("illegalChar_>", FileNameValidationResult.InvalidCharacter)]
    [InlineData("illegalChar_|", FileNameValidationResult.InvalidCharacter)]
    [InlineData("illegalChar_:", FileNameValidationResult.InvalidCharacter)]
    [InlineData("illegalChar_*", FileNameValidationResult.InvalidCharacter)]
    [InlineData("illegalChar_?", FileNameValidationResult.InvalidCharacter)]
    [InlineData(".", FileNameValidationResult.TrailingPeriod)]
    [InlineData("..", FileNameValidationResult.TrailingPeriod)]
    [InlineData("test....", FileNameValidationResult.TrailingPeriod)]
    [InlineData("test..", FileNameValidationResult.TrailingPeriod)]
    [InlineData("test.", FileNameValidationResult.TrailingPeriod)]
    [InlineData("con", FileNameValidationResult.WindowsReserved)]
    [InlineData("PRN", FileNameValidationResult.WindowsReserved)]
    [InlineData("AUX", FileNameValidationResult.WindowsReserved)]
    [InlineData("NUL", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM0", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM1", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM2", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM3", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM4", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM5", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM6", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM7", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM8", FileNameValidationResult.WindowsReserved)]
    [InlineData("COM9", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt0", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt1", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt2", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt3", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt4", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt5", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt6", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt7", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt8", FileNameValidationResult.WindowsReserved)]
    [InlineData("lpt9", FileNameValidationResult.WindowsReserved)]
    [InlineData("\\file", FileNameValidationResult.InvalidCharacter)]
    [InlineData("/file", FileNameValidationResult.InvalidCharacter)]
    [InlineData("|file", FileNameValidationResult.InvalidCharacter)]
    public void Test_IsValidFileName_InvalidNames(string input, FileNameValidationResult expected)
    {
        Assert.Equal(expected, _fileSystem.Path!.IsValidFileName(input));
    }

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