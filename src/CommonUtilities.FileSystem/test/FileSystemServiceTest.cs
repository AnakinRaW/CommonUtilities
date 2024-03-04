using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;


namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class FileSystemServiceTest
{
    private readonly MockFileSystem _fileSystem;

    public FileSystemServiceTest()
    {
        _fileSystem = new MockFileSystem();
    }

    [Theory]
    [InlineData("C:\\")]
    [InlineData("C:\\test")]
    [InlineData("C:\\test.txt")]
    [InlineData("test.txt")]
    public void TestGetDriveSize(string path)
    {
        var fsi = _fileSystem.FileInfo.New(path);
        var size = _service.GetDriveFreeSpace(fsi);
        Assert.True(size == 0);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void TestCreateTempFolder_Windows()
    {
        var dir = _service.CreateTemporaryFolderInTempWithRetry();
        Assert.NotNull(dir);
        Assert.StartsWith("C:\\temp\\", dir!.FullName, StringComparison.InvariantCultureIgnoreCase);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Linux)]
    public void TestCreateTempFolder_Linux()
    {
        var dir = _service.CreateTemporaryFolderInTempWithRetry();
        Assert.NotNull(dir);
        Assert.StartsWith("/temp/", dir!.FullName, StringComparison.InvariantCultureIgnoreCase);
    }


    [Fact]
    public void TestCreateFile()
    {
        _fileSystem.AddFile("C:\\test.txt", new MockFileData("test"));
        _service.CreateFileWithRetry("C:\\test.txt");
        Assert.NotEmpty(_fileSystem.File.ReadAllText("C:\\test.txt"));
        _service.CreateFileWithRetry("C:\\test2.txt");
        Assert.True(_fileSystem.FileExists("C:\\test2.txt"));
    }

    [Fact]
    public void TestCopyFile()
    {
        var fileToCopy = _fileSystem.FileInfo.New("C:\\test.txt");
        Assert.Throws<FileNotFoundException>(() => _service.CopyFileWithRetry(fileToCopy, "D:\\test.txt"));
        _fileSystem.AddFile("C:\\test.txt", new MockFileData("test"));
        _fileSystem.AddFile("C:\\test1.txt", new MockFileData(string.Empty));
        fileToCopy.Refresh();
        _service.CopyFileWithRetry(fileToCopy, "C:\\test1.txt");
        Assert.Equal("test", _fileSystem.File.ReadAllText("C:\\test1.txt"));
        Assert.True(_fileSystem.FileExists("C:\\test.txt"));
    }

    [Fact]
    public void TestMoveFile()
    {
        var fileToMove = _fileSystem.FileInfo.New("C:\\test.txt");
        Assert.Throws<FileNotFoundException>(() => _service.MoveFile(fileToMove, "D:\\test.txt", false));
        _fileSystem.AddFile("C:\\test.txt", new MockFileData("test"));
        _fileSystem.AddFile("C:\\test1.txt", new MockFileData(string.Empty));
        fileToMove.Refresh();
        Assert.Throws<IOException>(() => _service.MoveFile(fileToMove, "C:\\test1.txt", false));
        _service.MoveFile(fileToMove, "C:\\test1.txt", true);
        Assert.Equal("test", _fileSystem.File.ReadAllText("C:\\test1.txt"));
        Assert.False(_fileSystem.FileExists("C:\\test.txt"));
        _fileSystem.AddDirectory("D:\\");
        fileToMove = _fileSystem.FileInfo.New("C:\\test1.txt");
        _service.MoveFile(fileToMove, "D:\\test.txt", false);
        Assert.True(_fileSystem.FileExists("D:\\test.txt"));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void TestMoveDir_Windows()
    {
        var dirToMove = _fileSystem.DirectoryInfo.New("C:\\test");
        Assert.Throws<DirectoryNotFoundException>(() => _service.MoveDirectory(dirToMove, "C:\\test1", null, DirectoryOverwriteOption.NoOverwrite));
        _fileSystem.AddFile("C:\\test\\1.txt", new MockFileData("1"));
        _fileSystem.AddFile("C:\\test\\2.txt", new MockFileData("2"));
        _fileSystem.AddDirectory("C:\\test1");
        dirToMove.Refresh();
        Assert.Throws<IOException>(() => _service.MoveDirectory(dirToMove, "C:\\test1", null, DirectoryOverwriteOption.NoOverwrite));

        var delSuc = _service.MoveDirectory(dirToMove, "C:\\test1", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("C:\\test"));
        Assert.True(_fileSystem.Directory.Exists("C:\\test1"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("C:\\test1").GetFiles("*").Length);

        dirToMove = _fileSystem.DirectoryInfo.New("C:\\test1");
        _service.MoveDirectory(dirToMove, "D:\\test", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("D:\\test"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("D:\\test").GetFiles("*").Length);

        dirToMove = _fileSystem.DirectoryInfo.New("D:\\test");
        _fileSystem.AddFile("D:\\test1\\3.txt", new MockFileData("3"));
        _fileSystem.AddFile("D:\\test\\3.txt", new MockFileData("3"));
        _service.MoveDirectory(dirToMove, "D:\\test1", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("D:\\test1").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Linux)]
    public void TestMoveDir_Linux()
    {
        var dirToMove = _fileSystem.DirectoryInfo.New("/test");
        Assert.Throws<DirectoryNotFoundException>(() => _service.MoveDirectory(dirToMove, "/test1", null, DirectoryOverwriteOption.NoOverwrite));
        _fileSystem.AddFile("/test/1.txt", new MockFileData("1"));
        _fileSystem.AddFile("/test/2.txt", new MockFileData("2"));
        _fileSystem.AddDirectory("/test1");
        dirToMove.Refresh();
        Assert.Throws<IOException>(() => _service.MoveDirectory(dirToMove, "/test1", null, DirectoryOverwriteOption.NoOverwrite));

        var delSuc = _service.MoveDirectory(dirToMove, "/test1", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("/test"));
        Assert.True(_fileSystem.Directory.Exists("/test1"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("/test1").GetFiles("*").Length);

        dirToMove = _fileSystem.DirectoryInfo.New("/test1");
        _service.MoveDirectory(dirToMove, "/test", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("/test"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("/test").GetFiles("*").Length);

        dirToMove = _fileSystem.DirectoryInfo.New("/test");
        _fileSystem.AddFile("/test1/3.txt", new MockFileData("3"));
        _fileSystem.AddFile("/test/3.txt", new MockFileData("3"));
        _service.MoveDirectory(dirToMove, "/test1", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("/test1").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async void TestMoveDirAsync_Windows()
    {
        var dirToMove = _fileSystem.DirectoryInfo.New("C:\\test");
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await _service.MoveDirectoryAsync(dirToMove, "C:\\test1", null, DirectoryOverwriteOption.NoOverwrite));
        _fileSystem.AddFile("C:\\test\\1.txt", new MockFileData("1"));
        _fileSystem.AddFile("C:\\test\\2.txt", new MockFileData("2"));
        _fileSystem.AddDirectory("C:\\test1");
        dirToMove.Refresh();
        await Assert.ThrowsAsync<IOException>(async () => await _service.MoveDirectoryAsync(dirToMove, "C:\\test1", null, DirectoryOverwriteOption.NoOverwrite));

        var delSuc = await _service.MoveDirectoryAsync(dirToMove, "C:\\test1", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("C:\\test"));
        Assert.True(_fileSystem.Directory.Exists("C:\\test1"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("C:\\test1").GetFiles("*").Length);

        dirToMove = _fileSystem.DirectoryInfo.New("C:\\test1");
        await _service.MoveDirectoryAsync(dirToMove, "D:\\test", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("D:\\test"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("D:\\test").GetFiles("*").Length);

        dirToMove = _fileSystem.DirectoryInfo.New("D:\\test");
        _fileSystem.AddFile("D:\\test1\\3.txt", new MockFileData("3"));
        await _service.MoveDirectoryAsync(dirToMove, "D:\\test1", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("D:\\test1").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Linux)]
    public async void TestMoveDirAsync_Linux()
    {
        var dirToMove = _fileSystem.DirectoryInfo.New("/test");
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await _service.MoveDirectoryAsync(dirToMove, "/test1", null, DirectoryOverwriteOption.NoOverwrite));
        _fileSystem.AddFile("/test/1.txt", new MockFileData("1"));
        _fileSystem.AddFile("/test/2.txt", new MockFileData("2"));
        _fileSystem.AddDirectory("/test1");
        dirToMove.Refresh();
        await Assert.ThrowsAsync<IOException>(async () => await _service.MoveDirectoryAsync(dirToMove, "/test1", null, DirectoryOverwriteOption.NoOverwrite));

        var delSuc = await _service.MoveDirectoryAsync(dirToMove, "/test1", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("/test"));
        Assert.True(_fileSystem.Directory.Exists("/test1"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("/test1").GetFiles("*").Length);

        dirToMove = _fileSystem.DirectoryInfo.New("/test1");
        await _service.MoveDirectoryAsync(dirToMove, "/test", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("/test"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("/test").GetFiles("*").Length);

        dirToMove = _fileSystem.DirectoryInfo.New("/test");
        _fileSystem.AddFile("/test1/3.txt", new MockFileData("3"));
        await _service.MoveDirectoryAsync(dirToMove, "/test1", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("/test1").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void TestCopyDir_Windows()
    {
        var dirToCopy = _fileSystem.DirectoryInfo.New("C:\\test");
        Assert.Throws<DirectoryNotFoundException>(() => _service.CopyDirectory(dirToCopy, "C:\\test1", null, DirectoryOverwriteOption.NoOverwrite));
        _fileSystem.AddFile("C:\\test\\1.txt", new MockFileData("1"));
        _fileSystem.AddFile("C:\\test\\2.txt", new MockFileData("2"));
        _fileSystem.AddDirectory("C:\\test1");
        dirToCopy.Refresh();
        Assert.Throws<IOException>(() => _service.CopyDirectory(dirToCopy, "C:\\test1", null, DirectoryOverwriteOption.NoOverwrite));

        _service.CopyDirectory(dirToCopy, "C:\\test1", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("C:\\test"));
        Assert.True(_fileSystem.Directory.Exists("C:\\test1"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("C:\\test1").GetFiles("*").Length);

        dirToCopy = _fileSystem.DirectoryInfo.New("C:\\test1");
        _service.CopyDirectory(dirToCopy, "D:\\test", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("D:\\test"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("D:\\test").GetFiles("*").Length);

        dirToCopy = _fileSystem.DirectoryInfo.New("D:\\test");
        _fileSystem.AddFile("D:\\test1\\3.txt", new MockFileData("3"));
        _service.CopyDirectory(dirToCopy, "D:\\test1", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("D:\\test1").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Linux)]
    public void TestCopyDir_Linux()
    {
        var dirToCopy = _fileSystem.DirectoryInfo.New("/test");
        Assert.Throws<DirectoryNotFoundException>(() => _service.CopyDirectory(dirToCopy, "/test1", null, DirectoryOverwriteOption.NoOverwrite));
        _fileSystem.AddFile("/test/1.txt", new MockFileData("1"));
        _fileSystem.AddFile("/test/2.txt", new MockFileData("2"));
        _fileSystem.AddDirectory("/test1");
        dirToCopy.Refresh();
        Assert.Throws<IOException>(() => _service.CopyDirectory(dirToCopy, "/test1", null, DirectoryOverwriteOption.NoOverwrite));

        _service.CopyDirectory(dirToCopy, "/test1", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("/test"));
        Assert.True(_fileSystem.Directory.Exists("/test1"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("/test1").GetFiles("*").Length);

        dirToCopy = _fileSystem.DirectoryInfo.New("/test1");
        _service.CopyDirectory(dirToCopy, "/test", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("/test"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("/test").GetFiles("*").Length);

        dirToCopy = _fileSystem.DirectoryInfo.New("/test");
        _fileSystem.AddFile("/test1/3.txt", new MockFileData("3"));
        _service.CopyDirectory(dirToCopy, "/test1", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("/test1").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async void TestCopyDirAsync_Windows()
    {
        var dirToCopy = _fileSystem.DirectoryInfo.New("C:\\test");
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await _service.CopyDirectoryAsync(dirToCopy, "C:\\test1", null, DirectoryOverwriteOption.NoOverwrite));
        _fileSystem.AddFile("C:\\test\\1.txt", new MockFileData("1"));
        _fileSystem.AddFile("C:\\test\\2.txt", new MockFileData("2"));
        _fileSystem.AddDirectory("C:\\test1");
        dirToCopy.Refresh();
        await _service.CopyDirectoryAsync(dirToCopy, "C:\\test1", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("C:\\test"));
        Assert.True(_fileSystem.Directory.Exists("C:\\test1"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("C:\\test1").GetFiles("*").Length);

        dirToCopy = _fileSystem.DirectoryInfo.New("C:\\test1");
        await _service.CopyDirectoryAsync(dirToCopy, "D:\\test", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("D:\\test"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("D:\\test").GetFiles("*").Length);

        dirToCopy = _fileSystem.DirectoryInfo.New("D:\\test");
        _fileSystem.AddFile("D:\\test1\\3.txt", new MockFileData("3"));
        await _service.CopyDirectoryAsync(dirToCopy, "D:\\test1", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("D:\\test1").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Linux)]
    public async void TestCopyDirAsync_Linux()
    {
        var dirToCopy = _fileSystem.DirectoryInfo.New("/test");
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await _service.CopyDirectoryAsync(dirToCopy, "/test1", null, DirectoryOverwriteOption.NoOverwrite));
        _fileSystem.AddFile("/test/1.txt", new MockFileData("1"));
        _fileSystem.AddFile("/test/2.txt", new MockFileData("2"));
        _fileSystem.AddDirectory("/test1");

        dirToCopy.Refresh();
        await _service.CopyDirectoryAsync(dirToCopy, "/test1", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("/test"));
        Assert.True(_fileSystem.Directory.Exists("/test1"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("/test1").GetFiles("*").Length);
           
        dirToCopy = _fileSystem.DirectoryInfo.New("/test1");
        await _service.CopyDirectoryAsync(dirToCopy, "/test", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("/test"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("/test").GetFiles("*").Length);

        dirToCopy = _fileSystem.DirectoryInfo.New("/test");
        _fileSystem.AddFile("/test1/3.txt", new MockFileData("3"));
        await _service.CopyDirectoryAsync(dirToCopy, "/test1", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("/test1").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void TestDeleteFileTemp_Windows()
    {
        _fileSystem.AddFile("C:\\text.txt", new MockFileData(string.Empty));
        _fileSystem.AddFile("C:\\temp\\text.txt", new MockFileData(string.Empty));
        _fileSystem.AddFile("C:\\temp\\test\\text.txt", new MockFileData(string.Empty));

        var file1 = _fileSystem.FileInfo.New("C:\\text.txt");
        var file2 = _fileSystem.FileInfo.New("C:\\temp\\text.txt");
        var file3 = _fileSystem.FileInfo.New("C:\\temp\\test\\text.txt");

        _service.DeleteFileIfInTemp(file1);
        _service.DeleteFileIfInTemp(file2);
        _service.DeleteFileIfInTemp(file3);

        file1.Refresh();
        file2.Refresh();
        file3.Refresh();
        Assert.True(file1.Exists);
        Assert.False(file2.Exists);
        Assert.False(file3.Exists);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Linux)]
    public void TestDeleteFileTemp_Linux()
    {
        _fileSystem.AddFile("/text.txt", new MockFileData(string.Empty));
        _fileSystem.AddFile("/temp/text.txt", new MockFileData(string.Empty));
        _fileSystem.AddFile("/temp/test/text.txt", new MockFileData(string.Empty));

        var file1 = _fileSystem.FileInfo.New("/text.txt");
        var file2 = _fileSystem.FileInfo.New("/temp/text.txt");
        var file3 = _fileSystem.FileInfo.New("/temp/test/text.txt");

        _service.DeleteFileIfInTemp(file1);
        _service.DeleteFileIfInTemp(file2);
        _service.DeleteFileIfInTemp(file3);
        
        file1.Refresh();
        file2.Refresh();
        file3.Refresh();
        Assert.True(file1.Exists);
        Assert.False(file2.Exists);
        Assert.False(file3.Exists);
    }

    [Fact]
    public void TestDeleteFile()
    {
        _fileSystem.AddFile("text1.txt", new MockFileData(string.Empty));
        _fileSystem.AddFile("text2.txt", new MockFileData(string.Empty));

        var file1 = _fileSystem.FileInfo.New("text1.txt");
        var file2 = _fileSystem.FileInfo.New("text2.txt");
        file2.Attributes |= FileAttributes.ReadOnly;

        file1.Refresh();
        file2.Refresh();
        _service.DeleteFileWithRetry(file1);
        _service.DeleteFileWithRetry(file2);

        file1.Refresh();
        file2.Refresh();
        Assert.False(file1.Exists);
        Assert.False(file2.Exists);
    }

    [Fact]
    public void TestDeleteDir()
    {
        _fileSystem.AddFile("test/text1.txt", new MockFileData(string.Empty));
        _fileSystem.AddDirectory("C:/test1");

        var dir1 = _fileSystem.DirectoryInfo.New("test");
        var dir2 = _fileSystem.DirectoryInfo.New("test1");

        dir1.Refresh();
        Assert.Throws<IOException>(() => _service.DeleteDirectoryWithRetry(dir1, false));
        _service.DeleteDirectoryWithRetry(dir1);
        _service.DeleteDirectoryWithRetry(dir2);

        dir1.Refresh();
        dir2.Refresh();
        Assert.False(dir1.Exists);
        Assert.False(dir2.Exists);
    }
}