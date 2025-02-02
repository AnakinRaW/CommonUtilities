using System;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class IsDriveRelativePathTest
{
    private readonly IFileSystem _fileSystem = new MockFileSystem();

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("C", false)]
    [InlineData(@"C:\", false)]
    [InlineData("C:/", false)]
    [InlineData("C:/test", false)]
    [InlineData(@"C:\test", false)]
    [InlineData("/", false)]
    [InlineData(@"\", false)]
    [InlineData(@"\\Server\Share", false)]
    [InlineData(@"\\?\C:\", false)]
    [InlineData(@"\\?\C:", false)]
    [InlineData("C:", true, 'C')]
    [InlineData("c:", true, 'c')]
    [InlineData("X:", true, 'X')]
    [InlineData("ß:", false)]
    [InlineData("C:test", true, 'C')]
    [InlineData(@"C:test/a\a", true, 'C')]
    public void IsDriveRelative_Windows(string? path, bool expected, char? expectedDriveLetter = null)
    {
        Assert.Equal(expected, _fileSystem.Path.IsDriveRelative(path.AsSpan(), out var letter));
        Assert.Equal(expectedDriveLetter, letter);
        Assert.Equal(expected, _fileSystem.Path.IsDriveRelative(path, out letter));
        Assert.Equal(expectedDriveLetter, letter);
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("C", false)]
    [InlineData(@"C:\", false)]
    [InlineData("C:/", false)]
    [InlineData("C:/test", false)]
    [InlineData(@"C:\test", false)]
    [InlineData("/", false)]
    [InlineData(@"\", false)]
    [InlineData(@"\\Server\Share", false)]
    [InlineData(@"\\?\C:\", false)]
    [InlineData(@"\\?\C:", false)]
    [InlineData("C:", false)]
    [InlineData("c:", false)]
    [InlineData("X:", false)]
    [InlineData("ß:", false)]
    [InlineData("<:", false)]
    [InlineData("C:test", false)]
    [InlineData(@"C:test/a\a", false)]
    public void IsDriveRelative_Linux(string? path, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.IsDriveRelative(path.AsSpan(), out var letter));
        Assert.Null(letter);
        Assert.Equal(expected, _fileSystem.Path.IsDriveRelative(path, out letter));
        Assert.Null(letter);
    }
}