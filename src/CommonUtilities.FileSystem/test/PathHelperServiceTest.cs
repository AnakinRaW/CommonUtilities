#pragma warning disable CS0162
using System;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class PathHelperServiceTest
{
    private readonly PathHelperService _service;

    public PathHelperServiceTest()
    {
        _service = new PathHelperService(new MockFileSystem());
    }

    [Theory]
    [InlineData("C:\\a", "C:\\a\\b", "a\\b")]
    [InlineData("C:\\a\\", "D:\\a\\b", "D:\\a\\b")]
    [InlineData("C:\\a\\", "C:\\a\\b\\", "b\\")]
    [InlineData("C:\\a\\", "C:\\a\\b", "b")]
    [InlineData("C:\\a\\", "C:\\A\\B", "B")]
    [InlineData("C:\\a\\", "C:\\C\\B", "..\\C\\B")]
    [InlineData("a", "a\\b", "a\\b")]
    [InlineData("C:\\test.txt", "C:\\test\\abc.txt", "test\\abc.txt")]
    public void TestGetRelative_Windows(string basePath, string part, string expected)
    {
#if NET
        if (!OperatingSystem.IsWindows())
            return;
#endif
        Assert.Equal(expected, _service.GetRelativePath(basePath, part));
    }

    [Theory]
    [InlineData("/C:/a", "/C:/a/b", "a/b")]
    [InlineData("/C:/a/", "/D:/a/b", "../../D:/a/b")]
    [InlineData("/C:/a/", "/C:/a/b/", "b/")]
    [InlineData("/C:/a/", "/C:/a/b", "b")]
    [InlineData("/C:/a/", "/C:/A/B", "../A/B")]
    [InlineData("/C:/a/", "/C:/C/B", "../C/B")]
    [InlineData("a", "a/b", "a/b")]
    [InlineData("/C:/test.txt", "/C:/test/abc.txt", "test/abc.txt")]
    public void TestGetRelative_Linux(string basePath, string part, string expected)
    {
#if NET
        if (OperatingSystem.IsWindows())
#endif
            return;
        Assert.Equal(expected, _service.GetRelativePath(basePath, part));
    }

    [Theory]
    [InlineData("C:\\a", "C:\\a\\b", true)]
    [InlineData("C:\\a", "C:\\a", true)]
    [InlineData("C:\\a", "D:\\a", false)]
    [InlineData("C:\\a\\", "C:\\a\\b\\", true)]
    [InlineData("C:\\a\\", "C:\\a\\b", true)]
    [InlineData("C:\\a\\", "C:\\A\\B", true)]
    [InlineData("C:\\a\\", "C:\\C\\B", false)]
    [InlineData("a", "a\\b", true)]
    public void TestIsChild_Windows(string basePath, string candidate, bool expected)
    {
#if NET
        if (!OperatingSystem.IsWindows())
            return;
#endif
        Assert.Equal(expected, _service.IsChildOf(basePath, candidate));
    }

    [Theory]
    [InlineData("/C:/a", "/C:/a/b", true)]
    [InlineData("/C:/a", "/C:/a", true)]
    [InlineData("/C:/a", "/D:/a", false)]
    [InlineData("/C:/a/", "/C:/a/b/", true)]
    [InlineData("/C:/a/", "/C:/a/b", true)]
    [InlineData("/C:/a/", "/C:/A/B", false)]
    [InlineData("/C:/a/", "/C:/C/B", false)]
    [InlineData("a", "a/b", true)]
    public void TestIsChild_Linux(string basePath, string candidate, bool expected)
    {
#if NET
        if (OperatingSystem.IsWindows())
#endif
            return;
        Assert.Equal(expected, _service.IsChildOf(basePath, candidate));
    }

    [Theory]
    [InlineData("C:\\a", "C:\\a\\")]
    [InlineData("C:\\a\\", "C:\\a\\")]
    public void TestEnsureTrailing(string path, string expected)
    {
        Assert.Equal(expected, _service.EnsureTrailingSeparator(path));
    }

    [Theory]
    [InlineData("C:\\a", true)]
    [InlineData("C:\\a\\", true)]
    [InlineData("\\\\a\\", true)]
    [InlineData("..\\a\\", false)]
    [InlineData("a", false)]
    public void TestIsAbsolute_Windows(string path, bool expected)
    {
#if NET
        if (!OperatingSystem.IsWindows())
            return;
#endif
        Assert.Equal(expected, _service.IsAbsolute(path));
    }

    [Theory]
    [InlineData("/C:/a", true)]
    [InlineData("/C:/a/", true)]
    [InlineData("//a/", true)]
    [InlineData("../a/", false)]
    [InlineData("a", false)]
    public void TestIsAbsolute_Linux(string path, bool expected)
    {
#if NET
        if (OperatingSystem.IsWindows())
#endif
            return;
        Assert.Equal(expected, _service.IsAbsolute(path));
    }

    [Theory]
    [InlineData("C:\\a\\../A\\", PathNormalizeOptions.Full, "c:\\a")]
    [InlineData("C:\\a\\../A\\", PathNormalizeOptions.TrimTrailingSeparator, "C:\\a\\../A")]
    [InlineData("C:\\a\\../A\\", PathNormalizeOptions.ResolveFullPath, "C:\\A\\")]
    [InlineData("C:\\a\\../A\\", PathNormalizeOptions.ToLowerCase, "c:\\a\\../a\\")]
    [InlineData("C:\\a\\../A\\", PathNormalizeOptions.UnifySlashes, "C:\\a\\..\\A\\")]
    [InlineData("C:\\a\\\\a", PathNormalizeOptions.RemoveAdjacentSlashes, "C:\\a\\a")]
    public void NormalizeTest_Windows(string path, PathNormalizeOptions options, string expected)
    {
#if NET
        if (!OperatingSystem.IsWindows())
            return;
#endif
        Assert.Equal(expected, _service.NormalizePath(path, options));
    }

    [Theory]
    [InlineData("/C:\\a/../A\\", PathNormalizeOptions.Full, "/C:/A")]
    [InlineData("/C:/a/../A/", PathNormalizeOptions.TrimTrailingSeparator, "/C:/a/../A")]
    [InlineData("/C:/a/../A/", PathNormalizeOptions.ResolveFullPath, "/C:/A/")]
    [InlineData("/C:/a/A", PathNormalizeOptions.ToLowerCase, "/C:/a/A")]
    [InlineData("/C:\\a\\../A\\", PathNormalizeOptions.UnifySlashes, "/C:/a/../A/")]
    [InlineData("/C:/a//a", PathNormalizeOptions.RemoveAdjacentSlashes, "/C:/a/a")]
    public void NormalizeTest_Linux(string path, PathNormalizeOptions options, string expected)
    {
#if NET
        if (OperatingSystem.IsWindows())
#endif
            return;
        Assert.Equal(expected, _service.NormalizePath(path, options));
    }
}