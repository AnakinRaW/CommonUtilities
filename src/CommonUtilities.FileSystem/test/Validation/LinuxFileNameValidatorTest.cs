using System;
using System.Runtime.InteropServices;
using AnakinRaW.CommonUtilities.FileSystem.Validation;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test.Validation;

public class LinuxFileNameValidatorTest
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
    [InlineData("NUL.txt")]
    [InlineData("nameWithNonASCII_ö")]
    [InlineData("\u0160")]
    [InlineData("123\t")]
    [InlineData("123\r")]
    [InlineData("123\n")]
    [InlineData("nameWithTrailingSpace ")]
    [InlineData("   nameWithLeadingSpace")]
    [InlineData("my\\path")]
    [InlineData("illegalChar_<")]
    [InlineData("illegalChar_>")]
    [InlineData("illegalChar_|")]
    [InlineData("illegalChar_:")]
    [InlineData("illegalChar_*")]
    [InlineData("illegalChar_?")]
    [InlineData("\\file")]
    [InlineData("     ")]
    [InlineData("...")]
    public void IsValidFileName_ValidNames(string input)
    {
        Assert.Equal(FileNameValidationResult.Valid, LinuxFileNameValidator.Instance.IsValidFileName(input));
        Assert.Equal(FileNameValidationResult.Valid, LinuxFileNameValidator.Instance.IsValidFileName(input.AsSpan()));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Equal(FileNameValidationResult.Valid, CurrentSystemFileNameValidator.Instance.IsValidFileName(input));
            Assert.Equal(FileNameValidationResult.Valid, CurrentSystemFileNameValidator.Instance.IsValidFileName(input.AsSpan()));
        }
    }


    [Theory]
    [InlineData(null, FileNameValidationResult.NullOrEmpty)]
    [InlineData("", FileNameValidationResult.NullOrEmpty)]
    [InlineData(".", FileNameValidationResult.SystemReserved)]
    [InlineData("..", FileNameValidationResult.SystemReserved)]
    [InlineData("\0", FileNameValidationResult.InvalidCharacter)]
    [InlineData("123\0", FileNameValidationResult.InvalidCharacter)]
    [InlineData("/file", FileNameValidationResult.InvalidCharacter)]
    [InlineData("file/", FileNameValidationResult.InvalidCharacter)]
    public void IsValidFileName_InvalidNames(string? input, FileNameValidationResult expected)
    {
        Assert.Equal(expected, LinuxFileNameValidator.Instance.IsValidFileName(input));
        Assert.Equal(expected, LinuxFileNameValidator.Instance.IsValidFileName(input.AsSpan()));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Equal(expected, CurrentSystemFileNameValidator.Instance.IsValidFileName(input));
            Assert.Equal(expected, CurrentSystemFileNameValidator.Instance.IsValidFileName(input.AsSpan()));
        }
    }
}