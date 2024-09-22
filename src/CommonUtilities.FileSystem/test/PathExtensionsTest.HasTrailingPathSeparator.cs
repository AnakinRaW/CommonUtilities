using System;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class HasTrailingPathSeparatorTest
{
    // Using the actual file system here since we are not modifying it.
    // Also, we want to assure that everything works on the real system,
    // not that an arbitrary test implementation works.
    private readonly IFileSystem _fileSystem = new System.IO.Abstractions.FileSystem();
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Test_HasTrailingPathSeparator(string? input)
    {
        Assert.False(_fileSystem.Path.HasTrailingDirectorySeparator(input));
        Assert.False(_fileSystem.Path.HasTrailingDirectorySeparator(input.AsSpan()));
#if NET
        Assert.False(Path.EndsInDirectorySeparator(input));
#endif
    }

    public static TheoryData<string, bool> TestData_EndsInDirectorySeparator_Windows => new()
    {
        { @"\", true },
        { @"/", true },
        { @"C:\folder\", true },
        { @"C:/folder/", true },
        { @"C:\", true },
        { @"C:/", true },
        { @"\\", true },
        { @"//", true },
        { @"\\server\share\", true },
        { @"\\?\UNC\a\", true },
        { @"\\?\C:\", true },
        { @"\\?\UNC\", true },
        { @"folder\", true },
        { @"folder", false },
    };

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [MemberData(nameof(TestData_EndsInDirectorySeparator_Windows))]
    public void Test_HasTrailingPathSeparator_Windows(string input, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input));
        Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input.AsSpan()));
#if NET
        Assert.Equal(Path.EndsInDirectorySeparator(input), expected);
#endif
    }

    public static TheoryData<string, bool> TestData_EndsInDirectorySeparator_Linux => new()
    {
        { @"/", true },
        { @"/folder/", true },
        { @"//", true },
        { @"folder", false },
        { @"folder/", true }
    };

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [MemberData(nameof(TestData_EndsInDirectorySeparator_Linux))]
    public void Test_HasTrailingPathSeparator_Linux(string input, bool expected)
    {
        Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input));
        Assert.Equal(expected, _fileSystem.Path.HasTrailingDirectorySeparator(input.AsSpan()));
#if NET
        Assert.Equal(Path.EndsInDirectorySeparator(input), expected);
#endif
    }
}