using System;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class HasLeadingPathSeparatorTest
{
    // Using the actual file system here since we are not modifying it.
    // Also, we want to assure that everything works on the real system,
    // not that an arbitrary test implementation works.
    private readonly IFileSystem _fileSystem = new RealFileSystem();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void HasLeadingPathSeparator(string? input)
    {
        Assert.False(_fileSystem.Path.HasLeadingDirectorySeparator(input));
        Assert.False(_fileSystem.Path.HasLeadingDirectorySeparator(input.AsSpan()));
    }

    public static TheoryData<string, bool> TestData_StartsWithDirectorySeparator_Windows => new()
    {
        { @"\", true },
        { @"/", true },
        { @"C:\folder\", false },
        { @"C:/folder/", false },
        { @"C:\", false },
        { @"C:/", false },
        { @"\\", true },
        { @"//", true },
        { @"\\server\share\", true },
        { @"\\?\UNC\a\", true },
        { @"\\?\C:\", true },
        { @"\\?\UNC\", true },
        { @"\folder", true },
        { @"folder", false },
    };

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [MemberData(nameof(TestData_StartsWithDirectorySeparator_Windows))]
    public void HasLeadingPathSeparator_Windows(string input, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.HasLeadingDirectorySeparator(input));
        Assert.Equal(expected, _fileSystem.Path.HasLeadingDirectorySeparator(input.AsSpan()));
    }


    public static TheoryData<string, bool> TestData_StartsWithDirectorySeparator_Linux => new()
    {
        { @"/", true },
        { @"/folder/", true },
        { @"//", true },
        { @"folder", false },
        { @"/folder", true }
    };

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [MemberData(nameof(TestData_StartsWithDirectorySeparator_Linux))]
    public void HasLeadingPathSeparator_Linux(string input, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.HasLeadingDirectorySeparator(input));
        Assert.Equal(expected, _fileSystem.Path.HasLeadingDirectorySeparator(input.AsSpan()));
    }
}