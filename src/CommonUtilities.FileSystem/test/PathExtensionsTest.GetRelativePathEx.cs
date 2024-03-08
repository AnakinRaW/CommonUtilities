using System;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class GetRelativePathExTest
{
    private readonly MockFileSystem _fileSystem = new MockFileSystem();

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData(@"C:\", @"C:\", @".")]
    [InlineData(@"C:\a", @"C:\a\", @".")]
    [InlineData(@"C:\A", @"C:\a\", @".")]
    [InlineData(@"C:\a\", @"C:\a", @".")]
    [InlineData(@"C:\", @"C:\b", @"b")]
    [InlineData(@"C:\a", @"C:\b", @"..\b")]
    [InlineData(@"C:\a", @"C:\b\", @"..\b\")]
    [InlineData(@"C:\a\b", @"C:\a", @"..")]
    [InlineData(@"C:\a\b", @"C:\a\", @"..")]
    [InlineData(@"C:\a\b\", @"C:\a", @"..")]
    [InlineData(@"C:\a\b\", @"C:\a\", @"..")]
    [InlineData(@"C:\a\b\c", @"C:\a\b", @"..")]
    [InlineData(@"C:\a\b\c", @"C:\a\b\", @"..")]
    [InlineData(@"C:\a\b\c", @"C:\a", @"..\..")]
    [InlineData(@"C:\a\b\c", @"C:\a\", @"..\..")]
    [InlineData(@"C:\a\b\c\", @"C:\a\b", @"..")]
    [InlineData(@"C:\a\b\c\", @"C:\a\b\", @"..")]
    [InlineData(@"C:\a\b\c\", @"C:\a", @"..\..")]
    [InlineData(@"C:\a\b\c\", @"C:\a\", @"..\..")]
    [InlineData(@"C:\a\", @"C:\b", @"..\b")]
    [InlineData(@"C:\a", @"C:\a\b", @"b")]
    [InlineData(@"C:\a", @"C:\A\b", @"b")]
    [InlineData(@"C:\a", @"C:\b\c", @"..\b\c")]
    [InlineData(@"C:\a\", @"C:\a\b", @"b")]
    [InlineData(@"C:\", @"D:\", @"D:\")]
    [InlineData(@"C:\", @"D:\b", @"D:\b")]
    [InlineData(@"C:\", @"D:\b\", @"D:\b\")]
    [InlineData(@"C:\a", @"D:\b", @"D:\b")]
    [InlineData(@"C:\a\", @"D:\b", @"D:\b")]
    [InlineData(@"C:\ab", @"C:\a", @"..\a")]
    [InlineData(@"C:\a", @"C:\ab", @"..\ab")]
    [InlineData(@"C:\", @"\\LOCALHOST\Share\b", @"\\LOCALHOST\Share\b")]
    [InlineData(@"\\LOCALHOST\Share\a", @"\\LOCALHOST\Share\b", @"..\b")]
    // Tests which don't exist from .NET runtime
    [InlineData(@"C:\a", @"C:\a\.\.", @".")]
    public void Test_GetRelativePathEx_FromAbsolute_Windows(string root, string path, string expected)
    {
        var result = _fileSystem.Path.GetRelativePathEx(root, path);
        Assert.Equal(expected, result);

        Assert.Equal(
            _fileSystem.Path.GetFullPath(path).TrimEnd(_fileSystem.Path.DirectorySeparatorChar),
            _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_fileSystem.Path.GetFullPath(root), result))
                .TrimEnd(_fileSystem.Path.DirectorySeparatorChar),
            StringComparer.OrdinalIgnoreCase);

    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData(@"C:\a", @"b", @"b")]
    [InlineData(@"C:\a", @"a\b", @"a\b")]
    [InlineData(@"C:\a", @"a\..\b", @"a\..\b")]
    public void Test_GetRelativePathEx_FromRelative_Windows(string root, string path, string expected)
    {
        var result = _fileSystem.Path.GetRelativePathEx(root, path);
        Assert.Equal(expected, result);
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData(@"C:\", @"C:a", @"current\a")]
    [InlineData(@"C:\a", @"C:a", @"..\current\a")]
    [InlineData(@"C:\a", @"C:a\", @"..\current\a\")]
    [InlineData(@"C:\a\b", @"C:a\b", @"..\..\current\a\b")]
    [InlineData(@"C:\a", @"D:a", @"D:\a")]
    public void Test_GetRelativePathEx_FromDriveRelative_Windows(string root, string path, string expected)
    {
        _fileSystem.WithDrive("C:").WithDrive("D:");
        _fileSystem.Initialize().WithSubdirectory("C:\\current");
        _fileSystem.Directory.SetCurrentDirectory("C:\\current");
        var result = _fileSystem.Path.GetRelativePathEx(root, path);
        Assert.Equal(expected, result);

        Assert.Equal(
            _fileSystem.Path.GetFullPath(path).TrimEnd(_fileSystem.Path.DirectorySeparatorChar),
            _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_fileSystem.Path.GetFullPath(root), result))
                .TrimEnd(_fileSystem.Path.DirectorySeparatorChar),
            StringComparer.OrdinalIgnoreCase);
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData("/", @"/", @".")]
    [InlineData("/a", @"/a/", @".")]
    [InlineData("/a/", @"/a", @".")]
    [InlineData("/", @"/b", @"b")]
    [InlineData("/a", @"/b", @"../b")]
    [InlineData("/a/", @"/b", @"../b")]
    [InlineData("/a", @"/a/b", @"b")]
    [InlineData("/a", @"/b/c", @"../b/c")]
    [InlineData("/a/", @"/a/b", @"b")]
    [InlineData("/ab", @"/a", @"../a")]
    [InlineData("/a", @"/ab", @"../ab")]
    [InlineData("/a", @"/A/", @"../A/")]
    [InlineData("/a/", @"/A", @"../A")]
    [InlineData("/a/", @"/A/b", @"../A/b")]
    // Tests which don't exist from .NET runtime
    [InlineData(@"/a", @"/a/./.", @".")]
    public void Test_GetRelativePathEx_FromAbsolute_Linux(string root, string path, string expected)
    {
        var result = _fileSystem.Path.GetRelativePathEx(root, path);
        Assert.Equal(expected, result);

        Assert.Equal(
            _fileSystem.Path.GetFullPath(path).TrimEnd(_fileSystem.Path.DirectorySeparatorChar),
            _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_fileSystem.Path.GetFullPath(root), result))
                .TrimEnd(_fileSystem.Path.DirectorySeparatorChar),
            StringComparer.Ordinal);
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData("/a", "a", "a")]
    [InlineData("/a/b", "a/b", "a/b")]
    public void Test_GetRelativePathEx_FromRelative_Linux(string root, string path, string expected)
    {
        var result = _fileSystem.Path.GetRelativePathEx(root, path);
        Assert.Equal(expected, result);
    }
}