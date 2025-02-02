using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class GetFileNameTest
{
    private readonly IFileSystem _fileSystem = new RealFileSystem();

    public static TheoryData<string, string> TestData_GetFileName => new()
    {
        { ".", "." },
        { "..", ".." },
        { "file", "file" },
        { "file.", "file." },
        { "file.exe", "file.exe" },
        { " . ", " . " },
        { " .. ", " .. " },
        { "fi le", "fi le" },
        { Path.Combine("baz", "file.exe"), "file.exe" },
        { Path.Combine("baz", "file.exe") + Path.AltDirectorySeparatorChar, "" },
        { Path.Combine("bar", "baz", "file.exe"), "file.exe" },
        { Path.Combine("bar", "baz", "file.exe") + Path.DirectorySeparatorChar, "" }
    };


    [Theory, MemberData(nameof(TestData_GetFileName))]
    public void GetFileName_Span(string path, string expected)
    {
        PathAssert.Equal(expected.AsSpan(), _fileSystem.Path.GetFileName(path.AsSpan()));
        Assert.Equal(expected, _fileSystem.Path.GetFileName(path));
    }

    public static IEnumerable<object[]> TestData_GetFileName_Volume()
    {
        yield return [":", ":"];
        yield return [".:", ".:"];
        yield return [".:.", ".:."]; // Not a valid drive letter
        yield return ["file:", "file:"];
        yield return [":file", ":file"];
        yield return ["file:exe", "file:exe"];
        yield return [Path.Combine("baz", "file:exe"), "file:exe"];
        yield return [Path.Combine("bar", "baz", "file:exe"), "file:exe"];
    }

    [Theory, MemberData(nameof(TestData_GetFileName_Volume))]
    public void GetFileName_Volume(string path, string expected)
    {
        // We used to break on ':' on Windows. This is a valid file name character for alternate data streams.
        // Additionally, the character can show up on unix volumes mounted to Windows.
#if !NETFRAMEWORK
        Assert.Equal(expected, Path.GetFileName(path));
        Assert.Equal(expected, _fileSystem.Path.GetFileName(path));
#endif

        PathAssert.Equal(expected.AsSpan(), _fileSystem.Path.GetFileName(path.AsSpan()));
    }


    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData("B:", "")]
    [InlineData("A:.", ".")]
    public static void GetFileName_Windows(string path, string expected)
    {
        // With a valid drive letter followed by a colon, we have a root, but only on Windows.
        Assert.Equal(expected, Path.GetFileName(path));
    }

    public static TheoryData<string, string> TestData_GetFileNameWithoutExtension => new()
    {
        { "", "" },
        { "file", "file" },
        { "file.exe", "file" },
        { Path.Combine("bar", "baz", "file.exe"), "file" },
        { Path.Combine("bar", "baz") + Path.DirectorySeparatorChar, "" }
    };

    [Theory, MemberData(nameof(TestData_GetFileNameWithoutExtension))]
    public void GetFileNameWithoutExtension_Span(string path, string expected)
    {
        PathAssert.Equal(expected.AsSpan(), _fileSystem.Path.GetFileNameWithoutExtension(path.AsSpan()));
        Assert.Equal(expected, _fileSystem.Path.GetFileNameWithoutExtension(path));
    }
}