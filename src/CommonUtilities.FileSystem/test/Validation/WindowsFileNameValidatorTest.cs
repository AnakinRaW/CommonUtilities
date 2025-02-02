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
    public void IsValidFileName_ValidNames(string input)
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
    [InlineData("con", FileNameValidationResult.SystemReserved, true)]
    [InlineData("PRN", FileNameValidationResult.SystemReserved, true)]
    [InlineData("AUX", FileNameValidationResult.SystemReserved, true)]
    [InlineData("NUL", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM0", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM1", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM2", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM3", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM4", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM5", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM6", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM7", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM8", FileNameValidationResult.SystemReserved, true)]
    [InlineData("COM9", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt0", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt1", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt2", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt3", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt4", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt5", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt6", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt7", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt8", FileNameValidationResult.SystemReserved, true)]
    [InlineData("lpt9", FileNameValidationResult.SystemReserved, true)]
    [InlineData("\\file", FileNameValidationResult.InvalidCharacter)]
    [InlineData("/file", FileNameValidationResult.InvalidCharacter)]
    [InlineData("|file", FileNameValidationResult.InvalidCharacter)]
    public void IsValidFileName_InvalidNames(string? input, FileNameValidationResult expected, bool ignoreWhenNoWindowsReserved = false)
    {
        Assert.Equal(expected, WindowsFileNameValidator.Instance.IsValidFileName(input));
        Assert.Equal(expected, WindowsFileNameValidator.Instance.IsValidFileName(input.AsSpan(), true));
        Assert.Equal(ignoreWhenNoWindowsReserved ? FileNameValidationResult.Valid : expected,
            WindowsFileNameValidator.Instance.IsValidFileName(input.AsSpan(), false));
        
        Assert.Equal(expected, WindowsFileNameValidator.Instance.IsValidFileName(input.AsSpan()));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal(expected, CurrentSystemFileNameValidator.Instance.IsValidFileName(input));
            Assert.Equal(expected, CurrentSystemFileNameValidator.Instance.IsValidFileName(input.AsSpan()));
        }
    }
}