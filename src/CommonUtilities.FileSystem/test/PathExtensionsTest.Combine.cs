using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class CombineTest
{
    private readonly IFileSystem _fileSystem = new RealFileSystem();

    private static readonly char Separator = Path.DirectorySeparatorChar;

    public static IEnumerable<object[]> Combine_Basic_TestData()
    {
        yield return [Array.Empty<string>()];
        yield return [new[] { "abc" }];
        yield return [new[] { "abc", "def" }];
        yield return [new[] { "abc", "def", "ghi", "jkl", "mno" }];
        yield return [new[] { "abc" + Separator + "def", "def", "ghi", "jkl", "mno" }];

        // All paths are empty
        yield return [new[] { "" }];
        yield return [new[] { "", "" }];
        yield return [new[] { "", "", "" }];
        yield return [new[] { "", "", "", "" }];
        yield return [new[] { "", "", "", "", "" }];

        // Elements are all separated
        yield return [new[] { "abc" + Separator, "def" + Separator }];
        yield return [new[] { "abc" + Separator, "def" + Separator, "ghi" + Separator }];
        yield return [new[] { "abc" + Separator, "def" + Separator, "ghi" + Separator, "jkl" + Separator }];
        yield return [new[] { "abc" + Separator, "def" + Separator, "ghi" + Separator, "jkl" + Separator, "mno" + Separator }];
    }

    public static IEnumerable<string> Combine_ObscureWildcardCases_Input_TestData()
    {
        // Obscure wildcard characters
        yield return "\"";
        yield return "<";
        yield return ">";
    }

    public static IEnumerable<string> Combine_CommonCases_Input_TestData()
    {
        // Any path is rooted (starts with \, \\, A:)
        yield return Separator + "abc";
        yield return Separator + Separator + "abc";

        // Any path is empty (skipped)
        yield return "";

        // Any path is single element
        yield return "abc";
        yield return "abc" + Separator;

        // Any path is multiple element
        yield return Path.Combine("abc", Path.Combine("def", "ghi"));

        // Wildcard characters
        yield return "*";
        yield return "?";

#if !NETFRAMEWORK
        foreach (var testCase in Combine_ObscureWildcardCases_Input_TestData())
            yield return testCase;
#endif
    }

    public static IEnumerable<object[]> Combine_CommonCases_TestData()
    {
        foreach (var testPath in Combine_CommonCases_Input_TestData())
        {
            yield return [new[] { testPath }];

            yield return [new[] { "abc", testPath }];
            yield return [new[] { testPath, "abc" }];

            yield return [new[] { "abc", "def", testPath }];
            yield return [new[] { "abc", testPath, "def" }];
            yield return [new[] { testPath, "abc", "def" }];

            yield return [new[] { "abc", "def", "ghi", testPath }];
            yield return [new[] { "abc", "def", testPath, "ghi" }];
            yield return [new[] { "abc", testPath, "def", "ghi" }];
            yield return [new[] { testPath, "abc", "def", "ghi" }];

            yield return [new[] { "abc", "def", "ghi", "jkl", testPath }];
            yield return [new[] { "abc", "def", "ghi", testPath, "jkl" }];
            yield return [new[] { "abc", "def", testPath, "ghi", "jkl" }];
            yield return [new[] { "abc", testPath, "def", "ghi", "jkl" }];
            yield return [new[] { testPath, "abc", "def", "ghi", "jkl" }];
        }
    }

    [Theory]
    [MemberData(nameof(Combine_Basic_TestData))]
    [MemberData(nameof(Combine_CommonCases_TestData))]
    public void Combine(string[] paths)
    {
        var expected = string.Empty;
        if (paths.Length > 0) expected = paths[0];
        for (var i = 1; i < paths.Length; i++) 
            expected = _fileSystem.Path.Combine(expected, paths[i]);

        Assert.Equal(expected, _fileSystem.Path.Combine((ReadOnlySpan<string>)paths));
    }

    [Fact]
    public void PathIsNullWithoutRooted()
    {
        //any path is null without rooted after (ANE)
        CommonCasesException<ArgumentNullException>(null!);
    }

#if NETFRAMEWORK

    public static IEnumerable<object[]> Combine_IllegalCases_TestData()
    {
        foreach (var testPath in Combine_ObscureWildcardCases_Input_TestData())
        {
            yield return [new[] { testPath }];

            yield return [new[] { "abc", testPath }];
            yield return [new[] { testPath, "abc" }];

            yield return [new[] { "abc", "def", testPath }];
            yield return [new[] { "abc", testPath, "def" }];
            yield return [new[] { testPath, "abc", "def" }];

            yield return [new[] { "abc", "def", "ghi", testPath }];
            yield return [new[] { "abc", "def", testPath, "ghi" }];
            yield return [new[] { "abc", testPath, "def", "ghi" }];
            yield return [new[] { testPath, "abc", "def", "ghi" }];

            yield return [new[] { "abc", "def", "ghi", "jkl", testPath }];
            yield return [new[] { "abc", "def", "ghi", testPath, "jkl" }];
            yield return [new[] { "abc", "def", testPath, "ghi", "jkl" }];
            yield return [new[] { "abc", testPath, "def", "ghi", "jkl" }];
            yield return [new[] { testPath, "abc", "def", "ghi", "jkl" }];
        }
    }

    [Theory]
    [MemberData(nameof(Combine_IllegalCases_TestData))]
    public void Combine_IllegalChars_NetFx_Throws(string[] paths)
    {
        Assert.Throws<ArgumentException>(() => _fileSystem.Path.Combine((ReadOnlySpan<string>)paths));
    }

#endif

    private void CommonCasesException<T>(string testing) where T : Exception
    {
        VerifyException<T>([testing]);

        VerifyException<T>(["abc", testing]);
        VerifyException<T>([testing, "abc"]);

        VerifyException<T>(["abc", "def", testing]);
        VerifyException<T>(["abc", testing, "def"]);
        VerifyException<T>([testing, "abc", "def"]);

        VerifyException<T>(["abc", "def", "ghi", testing]);
        VerifyException<T>(["abc", "def", testing, "ghi"]);
        VerifyException<T>(["abc", testing, "def", "ghi"]);
        VerifyException<T>([testing, "abc", "def", "ghi"]);

        VerifyException<T>(["abc", "def", "ghi", "jkl", testing]);
        VerifyException<T>(["abc", "def", "ghi", testing, "jkl"]);
        VerifyException<T>(["abc", "def", testing, "ghi", "jkl"]);
        VerifyException<T>(["abc", testing, "def", "ghi", "jkl"]);
        VerifyException<T>([testing, "abc", "def", "ghi", "jkl"]);
    }

    private void VerifyException<T>(string[] paths) where T : Exception
    {
        Assert.Throws<T>(() => _fileSystem.Path.Combine((ReadOnlySpan<string>)paths));
    }
}