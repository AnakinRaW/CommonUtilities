using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Sklavenwalker.CommonUtilities.FileSystem;
using Xunit;

namespace Commonutilities.FileSystem.Test
{
    public class FileSystemServiceTest
    {
        private readonly FileSystemService _service;
        private readonly IFileSystem _fileSystem;

        public FileSystemServiceTest()
        {
            _fileSystem = new MockFileSystem();
            _service = new FileSystemService(_fileSystem);
        }

        [Theory]
        [InlineData("C:\\")]
        [InlineData("C:\\test")]
        [InlineData("C:\\test.txt")]
        [InlineData("test.txt")]
        public void TestGetDriveSize(string path)
        {
            var fsi = _fileSystem.FileInfo.FromFileName(path);
            var size = _service.GetDriveFreeSpace(fsi);
            Assert.True(size == 0);
        }
    }
}