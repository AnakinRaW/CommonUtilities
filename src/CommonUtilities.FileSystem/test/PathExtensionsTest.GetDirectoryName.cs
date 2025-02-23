using System;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class GetDirectoryNameTest
{
    private readonly IFileSystem _fileSystem = new RealFileSystem();

    public static TheoryData<string, string?> TestData_GetDirectoryName => new()
    {
        { ".", "" },
        { "..", "" },
        { "baz", "" },
        { Path.Combine("dir", "baz"), "dir" },
        { "dir.foo" + Path.AltDirectorySeparatorChar + "baz.txt", "dir.foo" },
        { Path.Combine("dir", "baz", "bar"), Path.Combine("dir", "baz") },
        { Path.Combine("..", "..", "files.txt"), Path.Combine("..", "..") },
        { Path.DirectorySeparatorChar + "foo", Path.DirectorySeparatorChar.ToString() },
        { Path.DirectorySeparatorChar.ToString(), null }
    };

    public static TheoryData<string> TestData_Spaces =>
    [
        " ",
        "   "
    ];

    public static TheoryData<string> TestData_EmbeddedNull => ["a\0b"];

    public static TheoryData<string> TestData_ControlChars =>
    [
        "\t",
        "\r\n",
        "\b",
        "\v",
        "\n"
    ];

    public static TheoryData<string> TestData_UnicodeWhiteSpace =>
    [
        "\u00A0", // Non-breaking Space
        "\u2028", // Line separator
        "\u2029" // Paragraph separator
    ];

    [Fact]
    public void GetDirectoryName_CurrentDirectory()
    {
        var curDir = Directory.GetCurrentDirectory();
        Assert.True(_fileSystem.Path.GetDirectoryName(_fileSystem.Path.GetPathRoot(curDir.AsSpan())).IsEmpty);
    }


    [Theory, MemberData(nameof(TestData_Spaces))]
    public void GetDirectoryName_Span_Spaces(string path)
    {
        PathAssert.Empty(_fileSystem.Path.GetDirectoryName(path.AsSpan()));
    }

    [Theory,
        MemberData(nameof(TestData_EmbeddedNull)),
        MemberData(nameof(TestData_ControlChars)),
        MemberData(nameof(TestData_UnicodeWhiteSpace))]
    public void GetDirectoryName_NetFxInvalid(string path)
    {
        PathAssert.Empty(_fileSystem.Path.GetDirectoryName(path.AsSpan()));
        PathAssert.Equal(path.AsSpan(), _fileSystem.Path.GetDirectoryName(_fileSystem.Path.Join(path, path).AsSpan()));
    }

    [Theory, MemberData(nameof(TestData_GetDirectoryName))]
    public void GetDirectoryName_Span(string path, string? expected)
    {
        PathAssert.Equal((expected ?? string.Empty).AsSpan(), _fileSystem.Path.GetDirectoryName(path.AsSpan()));
    }

    [Fact]
    public void GetDirectoryName_Span_CurrentDirectory()
    {
        var curDir = Directory.GetCurrentDirectory();
        PathAssert.Equal(curDir.AsSpan(), _fileSystem.Path.GetDirectoryName(_fileSystem.Path.Combine(curDir, "baz").AsSpan()));
        PathAssert.Empty(_fileSystem.Path.GetDirectoryName(_fileSystem.Path.GetPathRoot(curDir).AsSpan()));
    }

    [Fact]
    public void GetInvalidPathChars_Span()
    {
        Assert.All(Path.GetInvalidPathChars(), c =>
        {
            var bad = c.ToString();
            Assert.Equal(string.Empty, _fileSystem.Path.GetDirectoryName(bad.AsSpan()).ToString());
        });
    }
}