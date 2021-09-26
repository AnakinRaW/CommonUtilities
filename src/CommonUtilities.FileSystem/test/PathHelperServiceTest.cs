using Sklavenwalker.CommonUtilities.FileSystem;
using Xunit;

namespace Commonutilities.FileSystem.Test
{
    public class PathHelperServiceTest
    {
        private readonly PathHelperService _service;

        public PathHelperServiceTest()
        {
            _service = new PathHelperService();
        }

        [Theory]
        [InlineData("C:\\a", "C:\\a\\b", "a\\b")]
        [InlineData("C:\\a\\", "D:\\a\\b", "D:\\a\\b")]
        [InlineData("C:\\a\\", "C:\\a\\b\\", "b\\")]
        [InlineData("C:\\a\\", "C:\\a\\b", "b")]
        [InlineData("C:\\a\\", "C:\\A\\B", "B")]
        [InlineData("C:\\a\\", "C:\\C\\B", "..\\C\\B")]
        [InlineData("a", "a\\b", "a\\b")]
        public void TestGetRelative(string basePath, string part, string expected)
        {
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
        public void TestIsChild(string basePath, string candidate, bool expected)
        {
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
        public void TestIsAbsolute(string path, bool expected)
        {
            Assert.Equal(expected, _service.IsAbsolute(path));
        }

        [Theory]
        [InlineData("C:\\a\\../A\\", PathNormalizeOptions.Full, "c:\\a")]
        [InlineData("C:\\a\\../A\\", PathNormalizeOptions.TrimTrailingSeparator, "C:\\a\\../A")]
        [InlineData("C:\\a\\../A\\", PathNormalizeOptions.ResolveFullPath, "C:\\A\\")]
        [InlineData("C:\\a\\../A\\", PathNormalizeOptions.ToLowerCase, "c:\\a\\../a\\")]
        [InlineData("C:\\a\\../A\\", PathNormalizeOptions.UnifySlashes, "C:\\a\\..\\A\\")]
        [InlineData("C:\\a\\\\a", PathNormalizeOptions.RemoveAdjacentSlashes, "C:\\a\\a")]
        public void NormalizeTest(string path, PathNormalizeOptions options, string expected)
        {
            Assert.Equal(expected, _service.NormalizePath(path, options));
        }
    }
}
