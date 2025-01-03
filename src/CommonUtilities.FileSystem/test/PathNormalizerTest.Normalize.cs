using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class PathNormalizerTest
{
    [Fact]
    public void Test_Normalize_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            PathNormalizer.Normalize(null!, new PathNormalizeOptions());
        });
        Assert.Throws<ArgumentException>(() =>
        {
            PathNormalizer.Normalize(string.Empty, new PathNormalizeOptions());
        });

        Assert.Throws<ArgumentNullException>(() =>
        {
            PathNormalizer.Normalize(new ReadOnlySpan<char>(), new PathNormalizeOptions());
        });
        Assert.Throws<ArgumentException>(() =>
        {
            PathNormalizer.Normalize(string.Empty.AsSpan(), new PathNormalizeOptions());
        });
    }


    [Fact]
    public void Test_Normalize_Span_DefaultAndEmpty()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Span<char> buffer = new char[10];
            return PathNormalizer.Normalize(default, buffer, new PathNormalizeOptions());
        });
        Assert.Throws<ArgumentException>(() =>
        {
            Span<char> buffer = new char[10];
            return PathNormalizer.Normalize(string.Empty.AsSpan(), buffer, new PathNormalizeOptions());
        });
    }

    [Fact]
    public void Test_Normalize_Span_TooShort()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            const string value = "somePath";
            Span<char> buffer = new char[1];
            return PathNormalizer.Normalize(value.AsSpan(), buffer, new PathNormalizeOptions());
        });
    }

    [Fact]
    public void Test_Normalize()
    {
        foreach (var testData in NormalizeTestDataSource())
        {
            var result = PathNormalizer.Normalize(testData.Input, testData.Options);
            Assert.Equal(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? testData.ExpectedWindows : testData.ExpectedLinux,
                result);

            var resultFromRos = PathNormalizer.Normalize(testData.Input.AsSpan(), testData.Options);
            Assert.Equal(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? testData.ExpectedWindows : testData.ExpectedLinux,
                resultFromRos);

            // Just give it some space with +10
            Span<char> buffer = new char[testData.Input.Length + 10];
            var charsWritten = PathNormalizer.Normalize(testData.Input.AsSpan(), buffer, testData.Options);

            Assert.Equal(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? testData.ExpectedWindows : testData.ExpectedLinux,
                buffer.Slice(0, charsWritten).ToString());

        }
    }

    private static IEnumerable<NormalizeTestData> NormalizeTestDataSource()
    {
        // Default Options
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
            ExpectedLinux = "a/b\\C",
            ExpectedWindows = "a/b\\C",
            Options = default
        };

        // Default Dir separator unification
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b/C",
            ExpectedWindows = "a\\b\\C",
            Options =
                new PathNormalizeOptions
                {
                    UnifyDirectorySeparators = true
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b/C",
            ExpectedWindows = "a\\b\\C",
            Options = PathNormalizeOptions.UnifySeparators
        };

        // Forces Linux Dir separator unification
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b/C",
            ExpectedWindows = "a/b/C",
            Options =
                new PathNormalizeOptions
                {
                    UnifyDirectorySeparators = true,
                    UnifySeparatorKind = DirectorySeparatorKind.Linux
                }
        };

        // Forces Windows Dir separator unification
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a\\b\\C",
            ExpectedWindows = "a\\b\\C",
            Options =
                new PathNormalizeOptions
                {
                    UnifyDirectorySeparators = true,
                    UnifySeparatorKind = DirectorySeparatorKind.Windows
                }
        };

        // Lower only on case-insensitive system
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

        // Always lower-case
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

        // Upper only on case-insensitive system
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
            ExpectedLinux = "a/b\\C",
            ExpectedWindows = "A/B\\C",
            Options = PathNormalizeOptions.UnifyUpper
        };

        // Always upper-case
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
            Input = "a/b\\C",
            ExpectedLinux = "A/B\\C",
            ExpectedWindows = "A/B\\C",
            Options = PathNormalizeOptions.AlwaysUnifyUpper
        };

        // Trim trailing system dir separators 
        yield return new NormalizeTestData
        {
            Input = "a/b\\C//\\",
            ExpectedLinux = "a/b\\C//\\",
            ExpectedWindows = "a/b\\C",
            Options =
                new PathNormalizeOptions
                {
                    TrailingDirectorySeparatorBehavior = TrailingDirectorySeparatorBehavior.Trim
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C//\\",
            ExpectedLinux = "a/b\\C//\\",
            ExpectedWindows = "a/b\\C",
            Options = PathNormalizeOptions.TrimTrailingSeparators
        };

        // Trim trailing Windows dir separators 
        yield return new NormalizeTestData
        {
            Input = "a/b\\C//\\",
            ExpectedLinux = "a/b\\C",
            ExpectedWindows = "a/b\\C",
            Options =
                new PathNormalizeOptions
                {
                    TrailingDirectorySeparatorBehavior = TrailingDirectorySeparatorBehavior.Trim,
                    UnifySeparatorKind = DirectorySeparatorKind.Windows,
                }
        };

        // Trim trailing Linux dir separators 
        yield return new NormalizeTestData
        {
            Input = "a/b\\C//\\",
            ExpectedLinux = "a/b\\C//\\",
            ExpectedWindows = "a/b\\C//\\",
            Options =
                new PathNormalizeOptions
                {
                    TrailingDirectorySeparatorBehavior = TrailingDirectorySeparatorBehavior.Trim,
                    UnifySeparatorKind = DirectorySeparatorKind.Linux,
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C",
            ExpectedLinux = "a/b\\C/",
            ExpectedWindows = "a/b\\C\\",
            Options =
                new PathNormalizeOptions
                {
                    TrailingDirectorySeparatorBehavior = TrailingDirectorySeparatorBehavior.Ensure,
                    UnifySeparatorKind = DirectorySeparatorKind.Linux // Ensure this option is not altering the result
                }
        };
        yield return new NormalizeTestData
        {
            Input = "a/b\\C\\",
            ExpectedLinux = "a/b\\C\\/",
            ExpectedWindows = "a/b\\C\\",
            Options =
                new PathNormalizeOptions
                {
                    TrailingDirectorySeparatorBehavior = TrailingDirectorySeparatorBehavior.Ensure,
                    UnifySeparatorKind = DirectorySeparatorKind.Windows // Ensure this option is not altering the result
                }
        };


        // LongStrings
        yield return new NormalizeTestData
        {
            Input = new string('a', 300),
            ExpectedLinux = new string('a', 300),
            ExpectedWindows = new string('a', 300),
            Options = new PathNormalizeOptions()
        };
        yield return new NormalizeTestData
        {
            Input = new string('a', 300),
            ExpectedLinux = new string('a', 300) + "/",
            ExpectedWindows = new string('a', 300) + "\\",
            Options = new PathNormalizeOptions()
            {
                TrailingDirectorySeparatorBehavior = TrailingDirectorySeparatorBehavior.Ensure
            }
        };
        yield return new NormalizeTestData
        {
            Input = new string('a', 300) + "/",
            ExpectedLinux = new string('a', 300),
            ExpectedWindows = new string('a', 300),
            Options = new PathNormalizeOptions()
            {
                TrailingDirectorySeparatorBehavior = TrailingDirectorySeparatorBehavior.Trim
            }
        };
        yield return new NormalizeTestData
        {
            Input = new string('a', 300) + "/",
            ExpectedLinux = new string('A', 300) + "/",
            ExpectedWindows = new string('A', 300) + "/",
            Options = new PathNormalizeOptions
            {
                UnifyCase = UnifyCasingKind.UpperCaseForce
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