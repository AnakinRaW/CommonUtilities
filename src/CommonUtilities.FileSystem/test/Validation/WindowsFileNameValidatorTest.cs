using System;
using System.Runtime.InteropServices;
using AnakinRaW.CommonUtilities.FileSystem.Validation;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test.Validation;

public class WindowsFileNameValidatorTest
{
    [Theory]
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
        Assert.Equal(FileNameValidationResult.Valid, WindowsFileNameValidator.Instance.IsValidFileName(input));
        Assert.Equal(FileNameValidationResult.Valid, WindowsFileNameValidator.Instance.IsValidFileName(input.AsSpan()));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal(FileNameValidationResult.Valid,CurrentSystemFileNameValidator.Instance.IsValidFileName(input));
            Assert.Equal(FileNameValidationResult.Valid,CurrentSystemFileNameValidator.Instance.IsValidFileName(input.AsSpan()));
        }
    }


    [Theory]
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
    [InlineData("con", FileNameValidationResult.SystemReserved)]
    [InlineData("PRN", FileNameValidationResult.SystemReserved)]
    [InlineData("AUX", FileNameValidationResult.SystemReserved)]
    [InlineData("NUL", FileNameValidationResult.SystemReserved)]
    [InlineData("COM0", FileNameValidationResult.SystemReserved)]
    [InlineData("COM1", FileNameValidationResult.SystemReserved)]
    [InlineData("COM2", FileNameValidationResult.SystemReserved)]
    [InlineData("COM3", FileNameValidationResult.SystemReserved)]
    [InlineData("COM4", FileNameValidationResult.SystemReserved)]
    [InlineData("COM5", FileNameValidationResult.SystemReserved)]
    [InlineData("COM6", FileNameValidationResult.SystemReserved)]
    [InlineData("COM7", FileNameValidationResult.SystemReserved)]
    [InlineData("COM8", FileNameValidationResult.SystemReserved)]
    [InlineData("COM9", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt0", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt1", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt2", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt3", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt4", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt5", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt6", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt7", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt8", FileNameValidationResult.SystemReserved)]
    [InlineData("lpt9", FileNameValidationResult.SystemReserved)]
    [InlineData("\\file", FileNameValidationResult.InvalidCharacter)]
    [InlineData("/file", FileNameValidationResult.InvalidCharacter)]
    [InlineData("|file", FileNameValidationResult.InvalidCharacter)]
    public void Test_IsValidFileName_InvalidNames(string input, FileNameValidationResult expected)
    {
        Assert.Equal(expected, WindowsFileNameValidator.Instance.IsValidFileName(input));
        Assert.Equal(expected, WindowsFileNameValidator.Instance.IsValidFileName(input.AsSpan()));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal(expected, CurrentSystemFileNameValidator.Instance.IsValidFileName(input));
            Assert.Equal(expected, CurrentSystemFileNameValidator.Instance.IsValidFileName(input.AsSpan()));
        }
    }
}