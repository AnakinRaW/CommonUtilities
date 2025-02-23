using System;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class PathJoinTest
{
    private readonly IFileSystem _fileSystem = new RealFileSystem();

    protected static string Sep = Path.DirectorySeparatorChar.ToString();
    protected static string AltSep = Path.AltDirectorySeparatorChar.ToString();

    public static TheoryData<string?, string> TestData_JoinOnePath = new()
    {
            { "", "" },
            { Sep, Sep },
            { AltSep, AltSep },
            { "a", "a" },
            { null, "" }
        };

    public static TheoryData<string?, string?, string> TestData_JoinTwoPaths = new()
    {
            { "", "", "" },
            { Sep, "", Sep },
            { AltSep, "", AltSep },
            { "", Sep, Sep },
            { "", AltSep, AltSep },
            { Sep, Sep, $"{Sep}{Sep}" },
            { AltSep, AltSep, $"{AltSep}{AltSep}" },
            { "a", "", "a" },
            { "", "a", "a" },
            { "a", "a", $"a{Sep}a" },
            { $"a{Sep}", "a", $"a{Sep}a" },
            { "a", $"{Sep}a", $"a{Sep}a" },
            { $"a{Sep}", $"{Sep}a", $"a{Sep}{Sep}a" },
            { "a", $"a{Sep}", $"a{Sep}a{Sep}" },
            { $"a{AltSep}", "a", $"a{AltSep}a" },
            { "a", $"{AltSep}a", $"a{AltSep}a" },
            { $"a{Sep}", $"{AltSep}a", $"a{Sep}{AltSep}a" },
            { $"a{AltSep}", $"{AltSep}a", $"a{AltSep}{AltSep}a" },
            { "a", $"a{AltSep}", $"a{Sep}a{AltSep}" },
            { null, null, ""},
            { null, "a", "a"},
            { "a", null, "a"}
        };

    [Theory, MemberData(nameof(TestData_JoinTwoPaths))]
    public void JoinTwoPaths(string? path1, string? path2, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.Join(path1.AsSpan(), path2.AsSpan()));
        Assert.Equal(expected, _fileSystem.Path.Join(path1, path2));
    }

    [Theory, MemberData(nameof(TestData_JoinTwoPaths))]
    public void TryJoinTwoPaths(string? path1, string? path2, string expected)
    {
        var output = new char[expected.Length];

        Assert.True(_fileSystem.Path.TryJoin(path1.AsSpan(), path2.AsSpan(), output, out var written));
        Assert.Equal(expected.Length, written);
        Assert.Equal(expected, new string(output));

        if (expected.Length > 0)
        {
            Assert.False(_fileSystem.Path.TryJoin(path1.AsSpan(), path2.AsSpan(), Span<char>.Empty, out written));
            Assert.Equal(0, written);

            output = new char[expected.Length - 1];
            Assert.False(_fileSystem.Path.TryJoin(path1.AsSpan(), path2.AsSpan(), output, out written));
            Assert.Equal(0, written);
            Assert.Equal(output, new char[output.Length]);
        }
    }

    public static TheoryData<string?, string?, string?, string> TestData_JoinThreePaths = new()
    {
            { "", "", "", "" },
            { Sep, Sep, Sep, $"{Sep}{Sep}{Sep}" },
            { AltSep, AltSep, AltSep, $"{AltSep}{AltSep}{AltSep}" },
            { "a", "", "", "a" },
            { "", "a", "", "a" },
            { "", "", "a", "a" },
            { "a", "", "a", $"a{Sep}a" },
            { "a", "a", "", $"a{Sep}a" },
            { "", "a", "a", $"a{Sep}a" },
            { "a", "a", "a", $"a{Sep}a{Sep}a" },
            { "a", Sep, "a", $"a{Sep}a" },
            { $"a{Sep}", "", "a", $"a{Sep}a" },
            { $"a{Sep}", "a", "", $"a{Sep}a" },
            { "", $"a{Sep}", "a", $"a{Sep}a" },
            { "a", "", $"{Sep}a", $"a{Sep}a" },
            { $"a{AltSep}", "", "a", $"a{AltSep}a" },
            { $"a{AltSep}", "a", "", $"a{AltSep}a" },
            { "", $"a{AltSep}", "a", $"a{AltSep}a" },
            { "a", "", $"{AltSep}a", $"a{AltSep}a" },
            { null, null, null, "" },
            { "a", null, null, "a" },
            { null, "a", null, "a" },
            { null, null, "a", "a" },
            { "a", null, "a", $"a{Sep}a" }
        };

    [Theory, MemberData(nameof(TestData_JoinThreePaths))]
    public void JoinThreePaths(string? path1, string? path2, string? path3, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan()));
        Assert.Equal(expected, _fileSystem.Path.Join(path1, path2, path3));
    }

    [Theory, MemberData(nameof(TestData_JoinThreePaths))]
    public void TryJoinThreePaths(string? path1, string? path2, string? path3, string expected)
    {
        var output = new char[expected.Length];

        Assert.True(_fileSystem.Path.TryJoin(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), output, out var written));
        Assert.Equal(expected.Length, written);
        Assert.Equal(expected, new string(output));

        if (expected.Length > 0)
        {
            Assert.False(_fileSystem.Path.TryJoin(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), Span<char>.Empty, out written));
            Assert.Equal(0, written);

            output = new char[expected.Length - 1];
            Assert.False(_fileSystem.Path.TryJoin(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), output, out written));
            Assert.Equal(0, written);
            Assert.Equal(output, new char[output.Length]);
        }
    }

    public static TheoryData<string?, string?, string?, string?, string> TestData_JoinFourPaths = new()
    {
            { "", "", "", "", "" },
            { Sep, Sep, Sep, Sep, $"{Sep}{Sep}{Sep}{Sep}" },
            { AltSep, AltSep, AltSep, AltSep, $"{AltSep}{AltSep}{AltSep}{AltSep}" },
            { "a", "", "", "", "a" },
            { "", "a", "", "", "a" },
            { "", "", "a", "", "a" },
            { "", "", "", "a", "a" },
            { "a", "b", "", "", $"a{Sep}b" },
            { "a", "", "b", "", $"a{Sep}b" },
            { "a", "", "", "b", $"a{Sep}b" },
            { "a", "b", "c", "", $"a{Sep}b{Sep}c" },
            { "a", "b", "", "c", $"a{Sep}b{Sep}c" },
            { "a", "", "b", "c", $"a{Sep}b{Sep}c" },
            { "", "a", "b", "c", $"a{Sep}b{Sep}c" },
            { "a", "b", "c", "d", $"a{Sep}b{Sep}c{Sep}d" },
            { "a", Sep, "b", "", $"a{Sep}b" },
            { "a", Sep, "", "b", $"a{Sep}b" },
            { "a", "", Sep, "b", $"a{Sep}b" },
            { $"a{Sep}", "b", "", "", $"a{Sep}b" },
            { $"a{Sep}", "", "b", "", $"a{Sep}b" },
            { $"a{Sep}", "", "", "b", $"a{Sep}b" },
            { "", $"a{Sep}", "b", "", $"a{Sep}b" },
            { "", $"a{Sep}", "", "b", $"a{Sep}b" },
            { "", "", $"a{Sep}", "b", $"a{Sep}b" },
            { "a", $"{Sep}b", "", "", $"a{Sep}b" },
            { "a", "", $"{Sep}b", "", $"a{Sep}b" },
            { "a", "", "", $"{Sep}b", $"a{Sep}b" },
            { $"{Sep}a", "", "", "", $"{Sep}a" },
            { "", $"{Sep}a", "", "", $"{Sep}a" },
            { "", "", $"{Sep}a", "", $"{Sep}a" },
            { "", "", "", $"{Sep}a", $"{Sep}a" },
            { $"{Sep}a", "b", "", "", $"{Sep}a{Sep}b" },
            { "", $"{Sep}a", "b", "", $"{Sep}a{Sep}b" },
            { "", "", $"{Sep}a", "b", $"{Sep}a{Sep}b" },
            { $"a{Sep}", $"{Sep}b", "", "", $"a{Sep}{Sep}b" },
            { $"a{Sep}", "", $"{Sep}b", "", $"a{Sep}{Sep}b" },
            { $"a{Sep}", "", "", $"{Sep}b", $"a{Sep}{Sep}b" },
            { $"a{AltSep}", "b", "", "", $"a{AltSep}b" },
            { $"a{AltSep}", "", "b", "", $"a{AltSep}b" },
            { $"a{AltSep}", "", "", "b", $"a{AltSep}b" },
            { "", $"a{AltSep}", "b", "", $"a{AltSep}b" },
            { "", $"a{AltSep}", "", "b", $"a{AltSep}b" },
            { "", "", $"a{AltSep}", "b", $"a{AltSep}b" },
            { "a", $"{AltSep}b", "", "", $"a{AltSep}b" },
            { "a", "", $"{AltSep}b", "", $"a{AltSep}b" },
            { "a", "", "", $"{AltSep}b", $"a{AltSep}b" },
            { null, null, null, null, "" },
            { "a", null, null, null, "a" },
            { null, "a", null, null, "a" },
            { null, null, "a", null, "a" },
            { null, null, null, "a", "a" },
            { "a", null, "b", null, $"a{Sep}b" },
            { "a", null, null, "b", $"a{Sep}b" }
        };

    [Theory, MemberData(nameof(TestData_JoinFourPaths))]
    public void JoinFourPaths(string? path1, string? path2, string? path3, string? path4, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), path4.AsSpan()));
        Assert.Equal(expected, _fileSystem.Path.Join(path1, path2, path3, path4));
    }

    [Fact]
    public void JoinStringArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _fileSystem.Path.Join(null!));
    }

    [Fact]
    public void JoinStringArray_ZeroLengthArray()
    {
        Assert.Equal(string.Empty, _fileSystem.Path.Join(Array.Empty<string>()));
        Assert.Equal(string.Empty, _fileSystem.Path.Join((ReadOnlySpan<string?>)Array.Empty<string>()));
    }

    [Theory, MemberData(nameof(TestData_JoinOnePath))]
    public void JoinStringArray_1(string? path1, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.Join(new[] { path1 }));
        Assert.Equal(expected, _fileSystem.Path.Join((ReadOnlySpan<string?>)new[] { path1 }));
    }

    [Theory, MemberData(nameof(TestData_JoinTwoPaths))]
    public void JoinStringArray_2(string? path1, string? path2, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.Join(new[] { path1, path2 }));
        Assert.Equal(expected, _fileSystem.Path.Join((ReadOnlySpan<string?>)new[] { path1, path2 }));
    }

    [Theory, MemberData(nameof(TestData_JoinThreePaths))]
    public void JoinStringArray_3(string? path1, string? path2, string? path3, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.Join(new[] { path1, path2, path3 }));
        Assert.Equal(expected, _fileSystem.Path.Join((ReadOnlySpan<string?>)new[] { path1, path2, path3 }));
    }

    [Theory, MemberData(nameof(TestData_JoinFourPaths))]
    public void JoinStringArray_4(string? path1, string? path2, string? path3, string? path4, string expected)
    {
        Assert.Equal(expected, _fileSystem.Path.Join(new[] { path1, path2, path3, path4 }));
        Assert.Equal(expected, _fileSystem.Path.Join(((ReadOnlySpan<string?>)new[] { path1, path2, path3, path4 })));
    }

    [Theory, MemberData(nameof(TestData_JoinFourPaths))]
    public void JoinStringArray_8(string? path1, string? path2, string? path3, string? path4, string fourJoined)
    {
        Assert.Equal(_fileSystem.Path.Join(fourJoined, fourJoined),
            _fileSystem.Path.Join(path1, path2, path3, path4, path1, path2, path3, path4));

        Assert.Equal(_fileSystem.Path.Join(fourJoined, fourJoined),
            _fileSystem.Path.Join((ReadOnlySpan<string?>)new[] { path1, path2, path3, path4, path1, path2, path3, path4 }));
    }
}