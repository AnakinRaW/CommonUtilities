using System;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class GetPathRootTest
{
    private readonly IFileSystem _fileSystem = new System.IO.Abstractions.FileSystem();

    [Fact]
    public void GetPathRoot_Empty_Span()
    {
        PathAssert.Empty(_fileSystem.Path.GetPathRoot(ReadOnlySpan<char>.Empty));
    }

    [Fact]
    public void GetPathRoot_Basic()
    {
        var cwd = Directory.GetCurrentDirectory();
        var substring = cwd.Substring(0, cwd.IndexOf(Path.DirectorySeparatorChar) + 1);

        Assert.Equal(substring, _fileSystem.Path.GetPathRoot(cwd));
        PathAssert.Equal(substring.AsSpan(), _fileSystem.Path.GetPathRoot(cwd.AsSpan()));

        Assert.True(_fileSystem.Path.IsPathRooted(cwd));

        Assert.Equal(string.Empty, _fileSystem.Path.GetPathRoot(@"file.exe"));
        Assert.True(_fileSystem.Path.GetPathRoot(@"file.exe".AsSpan()).IsEmpty);

        Assert.False(_fileSystem.Path.IsPathRooted("file.exe"));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData(@"/../../.././tmp/..")]
    [InlineData(@"/../../../")]
    [InlineData(@"/../../../tmp/bar/..")]
    [InlineData(@"/../.././././bar/../../../")]
    [InlineData(@"/../../././tmp/..")]
    [InlineData(@"/../../tmp/../../")]
    [InlineData(@"/../../tmp/bar/..")]
    [InlineData(@"/../tmp/../..")]
    [InlineData(@"/././../../../../")]
    [InlineData(@"/././../../../")]
    [InlineData(@"/./././bar/../../../")]
    [InlineData(@"/")]
    [InlineData(@"/bar")]
    [InlineData(@"/bar/././././../../..")]
    [InlineData(@"/bar/tmp")]
    [InlineData(@"/tmp/..")]
    [InlineData(@"/tmp/../../../../../bar")]
    [InlineData(@"/tmp/../../../bar")]
    [InlineData(@"/tmp/../bar/../..")]
    [InlineData(@"/tmp/bar")]
    [InlineData(@"/tmp/bar/..")]
    public void GePathRoot_Unix(string path)
    {
        var expected = @"/";
        Assert.Equal(expected, _fileSystem.Path.GetPathRoot(path));
        PathAssert.Equal(expected.AsSpan(), _fileSystem.Path.GetPathRoot(path.AsSpan()));
    }

    public static TheoryData<string, string> TestData_GetPathRoot_Windows => new()
    {
        { @"C:", @"C:" },
        { @"C:\", @"C:\" },
        { @"C:\\", @"C:\" },
        { @"C:\foo1", @"C:\" },
        { @"C:\\foo2", @"C:\" },
    };

    public static TheoryData<string, string> TestData_GetPathRoot_Unc => new()
    {
        { @"\\test\unc\path\to\something", @"\\test\unc" },
        { @"\\a\b\c\d\e", @"\\a\b" },
        { @"\\a\b\", @"\\a\b" },
        { @"\\a\b", @"\\a\b" },
        { @"\\test\unc", @"\\test\unc" },
    };

    public static TheoryData<string, string> TestData_GetPathRoot_DevicePaths => new()
    {
        { @"\\?\UNC\test\unc\path\to\something", @"\\?\UNC\test\unc" },
        { @"\\?\UNC\test\unc", @"\\?\UNC\test\unc" },
        { @"\\?\UNC\a\b1", @"\\?\UNC\a\b1" },
        { @"\\?\UNC\a\b2\", @"\\?\UNC\a\b2" },
        { @"\\?\C:\foo\bar.txt", @"\\?\C:\" }
    };

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [MemberData(nameof(TestData_GetPathRoot_Windows))]
    [MemberData(nameof(TestData_GetPathRoot_Unc))]
    [MemberData(nameof(TestData_GetPathRoot_DevicePaths))]
    public void GetPathRoot_Span(string value, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.GetPathRoot(value));

        Assert.Equal(expected, _fileSystem.Path.GetPathRoot(value.AsSpan()).ToString());
        Assert.True(_fileSystem.Path.IsPathRooted(value.AsSpan()));
    }
}