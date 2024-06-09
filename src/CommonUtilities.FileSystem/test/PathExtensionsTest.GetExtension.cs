using System;
using System.IO.Abstractions;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class GetExtensionTest
{
    private readonly IFileSystem _fileSystem = new System.IO.Abstractions.FileSystem();

    public static TheoryData<string, string> TestData_GetExtension => new()
    {
        { @"file.exe", ".exe" },
        { @"file", "" },
        { @"file.", "" },
        { @"file.s", ".s" },
        { @"test/file", "" },
        { @"test/file.extension", ".extension" },
        { @"test\file", "" },
        { @"test\file.extension", ".extension" },
        { "file.e xe", ".e xe"},
        { "file. ", ". "},
        { " file. ", ". "},
        { " file.extension", ".extension"}
    };

    [Theory, MemberData(nameof(TestData_GetExtension))]
    public void GetExtension_Span(string path, string expected)
    {
        PathAssert.Equal(expected.AsSpan(), _fileSystem.Path.GetExtension(path.AsSpan()));
        Assert.Equal(expected, _fileSystem.Path.GetExtension(path));
        Assert.Equal(!string.IsNullOrEmpty(expected), _fileSystem.Path.HasExtension(path.AsSpan()));
        Assert.Equal(!string.IsNullOrEmpty(expected), _fileSystem.Path.HasExtension(path));
    }
}