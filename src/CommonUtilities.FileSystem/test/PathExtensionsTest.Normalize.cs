using System.Collections.Generic;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class NormalizePathTest
{
    private readonly IFileSystem _fileSystem = new MockFileSystem();

    [Fact]
    public void Test_Normalize()
    {
        foreach (var testData in NormalizeTestDataSource())
        {
            var result = _fileSystem.Path.Normalize(testData.Input, testData.Options);
            Assert.Equal(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? testData.ExpectedWindows : testData.ExpectedLinux,
                result);
        }

    }

    private static IEnumerable<NormalizeTestData> NormalizeTestDataSource()
    {
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b\\C",
            ExpectedWindows = "a/b\\C",
            Options = new PathNormalizeOptions()
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b/C",
            ExpectedWindows = "a\\b\\C",
            Options =
                new PathNormalizeOptions
                {
                    UnifySlashes = true
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b/C",
            ExpectedWindows = "a/b/C",
            Options =
                new PathNormalizeOptions
                {
                    UnifySlashes = true,
                    SeparatorKind = DirectorySeparatorKind.Linux
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a\\b\\C",
            ExpectedWindows = "a\\b\\C",
            Options =
                new PathNormalizeOptions
                {
                    UnifySlashes = true,
                    SeparatorKind = DirectorySeparatorKind.Windows
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b\\C",
            ExpectedWindows = "a/b\\c",
            Options =
                new PathNormalizeOptions
                {
                    UnifyCase = UnifyCasingKind.LowerCase
                }
        };

        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b\\c",
            ExpectedWindows = "a/b\\c",
            Options =
                new PathNormalizeOptions
                {
                    UnifyCase = UnifyCasingKind.LowerCaseForce
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b\\C",
            ExpectedWindows = "A/B\\C",
            Options =
                new PathNormalizeOptions
                {
                    UnifyCase = UnifyCasingKind.UpperCase
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "A/B\\C",
            ExpectedWindows = "A/B\\C",
            Options =
                new PathNormalizeOptions
                {
                    UnifyCase = UnifyCasingKind.UpperCaseForce
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C//\\",
            ExpectedLinux = "a/b\\C//\\",
            ExpectedWindows = "a/b\\C",
            Options =
                new PathNormalizeOptions
                {
                    TrimTrailingSeparator = true
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C//\\",
            ExpectedLinux = "a/b\\C",
            ExpectedWindows = "a/b\\C",
            Options =
                new PathNormalizeOptions
                {
                    TrimTrailingSeparator = true,
                    SeparatorKind = DirectorySeparatorKind.Windows,
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C//\\",
            ExpectedLinux = "a/b\\C//\\",
            ExpectedWindows = "a/b\\C//\\",
            Options =
                new PathNormalizeOptions
                {
                    TrimTrailingSeparator = true,
                    SeparatorKind = DirectorySeparatorKind.Linux,
                }
        };
    }

    internal record NormalizeTestData
    {
        public required string Input { get; init; }

        public string? ExpectedWindows { get; init; }

        public string? ExpectedLinux { get; init; }

        public required PathNormalizeOptions Options { get; init; }
    }
}