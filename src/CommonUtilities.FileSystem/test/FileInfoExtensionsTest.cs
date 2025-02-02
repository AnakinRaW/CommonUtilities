using System.IO;
using System.Runtime.InteropServices;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class FileInfoExtensionsTest
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void DeleteIfInTemp()
    {
        _fileSystem.Initialize();

        var tempPath = _fileSystem.Path.GetTempPath();

        _fileSystem.Initialize()
            .WithFile("test.txt")
            .WithFile(_fileSystem.Path.Combine(tempPath, "test.txt"))
            .WithFile(_fileSystem.Path.Combine(tempPath, "path", "test.txt"));
        
        var file1 = _fileSystem.FileInfo.New("test.txt");
        var file2 = _fileSystem.FileInfo.New(_fileSystem.Path.Combine(tempPath, "test.txt"));
        var file3 = _fileSystem.FileInfo.New(_fileSystem.Path.Combine(tempPath, "path", "test.txt"));
        var file4 = _fileSystem.FileInfo.New("notExisting.txt");

        
        file1.DeleteIfInTemp();
        file2.DeleteIfInTemp();
        file3.DeleteIfInTemp();
        file4.DeleteIfInTemp();

        file1.Refresh();
        file2.Refresh();
        file3.Refresh();

        Assert.True(file1.Exists);
        Assert.False(file2.Exists);
        Assert.False(file3.Exists);
    }

    [Fact]
    public void DeleteWithRetry()
    {
        _fileSystem.Initialize()
            .WithFile("text1.txt")
            .WithFile("text2.txt");

        var file1 = _fileSystem.FileInfo.New("text1.txt");
        var file2 = _fileSystem.FileInfo.New("text2.txt");
        file2.Attributes |= FileAttributes.ReadOnly;

        // https://github.com/dotnet/runtime/issues/52700
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var fs1 = _fileSystem.FileStream.New(file1.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fs2 = _fileSystem.FileStream.New(file2.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            Assert.Throws<IOException>(() => file1.DeleteWithRetry());
            Assert.Throws<IOException>(() => _fileSystem.File.DeleteWithRetry(file2.FullName));

            file1.Refresh();
            file2.Refresh();
            Assert.True(file1.Exists);
            Assert.True(file2.Exists);

            fs1.Dispose();
            fs2.Dispose();
        }



        file1.Refresh();
        file2.Refresh();
        file1.DeleteWithRetry();
        _fileSystem.File.TryDeleteWithRetry(file2.FullName);


        file1.Refresh();
        file2.Refresh();
        Assert.False(file1.Exists);
        Assert.False(file2.Exists);
    }

    [Fact]
    public void TryDeleteWithRetry()
    {
        _fileSystem.Initialize()
            .WithFile("text1.txt")
            .WithFile("text2.txt");

        var file1 = _fileSystem.FileInfo.New("text1.txt");
        var file2 = _fileSystem.FileInfo.New("text2.txt");
        file2.Attributes |= FileAttributes.ReadOnly;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var fs1 = _fileSystem.FileStream.New(file1.FullName, FileMode.Open, FileAccess.Read, FileShare.None);
            var fs2 = _fileSystem.FileStream.New(file2.FullName, FileMode.Open, FileAccess.Read, FileShare.None);
            Assert.False(file1.TryDeleteWithRetry());
            Assert.False(_fileSystem.File.TryDeleteWithRetry(file2.FullName));

            file1.Refresh();
            file2.Refresh();
            Assert.True(file1.Exists);
            Assert.True(file2.Exists);

            fs1.Dispose();
            fs2.Dispose();
        }
        
        file1.Refresh();
        file2.Refresh(); 
        Assert.True(file1.TryDeleteWithRetry());
        Assert.True(_fileSystem.File.TryDeleteWithRetry(file2.FullName));

        file1.Refresh();
        file2.Refresh();
        Assert.False(file1.Exists);
        Assert.False(file2.Exists);
    }

    [Fact]
    public void CopyWithRetry_ThrowsFileNotFound()
    {
        _fileSystem.Initialize();
        var fileToCopy = _fileSystem.FileInfo.New("test.txt");
        Assert.Throws<FileNotFoundException>(() => fileToCopy.CopyWithRetry("test1.txt"));
    }

    [Fact]
    public void CopyWithRetry()
    {
        _fileSystem.Initialize().WithFile("test.txt").Which(f => f.HasStringContent("1234"));
        _fileSystem.Initialize().WithFile("test1.txt");

        var fi = _fileSystem.FileInfo.New("test.txt");
        fi.CopyWithRetry("test1.txt");
        Assert.Equal("1234", _fileSystem.File.ReadAllText("test1.txt"));
        Assert.True(_fileSystem.File.Exists("test.txt"));
    }


    [Fact]
    public void MoveToEx_ThrowsFileNotFoundException()
    {
        var fileToMove = _fileSystem.FileInfo.New("test.txt");
        Assert.Throws<FileNotFoundException>(() => fileToMove.MoveToEx("test.txt", false));
    }


    [Fact]
    public void MoveToEx_NoOverwrite_ThrowsIOException()
    {
        _fileSystem.Initialize().WithFile("test.txt").Which(f => f.HasStringContent("1234"));
        _fileSystem.Initialize().WithFile("test1.txt");

        var fileToMove = _fileSystem.FileInfo.New("test.txt");
        Assert.Throws<IOException>(() => fileToMove.MoveToEx("test1.txt", false));
    }

    [Fact]
    public void MoveToEx_WithOverwrite()
    {

        _fileSystem.Initialize().WithFile("test.txt").Which(f => f.HasStringContent("1234"));
        _fileSystem.Initialize().WithFile("test1.txt");

        var fileToMove = _fileSystem.FileInfo.New("test.txt");

        fileToMove.MoveToEx("test1.txt", true);
        Assert.Equal("1234", _fileSystem.File.ReadAllText("test1.txt"));
        Assert.False(_fileSystem.File.Exists("test.txt"));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void MoveToEx_AcrossVolume_Windows()
    {
        _fileSystem.WithDrive("D:");
        _fileSystem.Initialize().WithFile("test.txt").Which(f => f.HasStringContent("1234"));
        _fileSystem.Initialize().WithFile("test1.txt");

        var fileToMove = _fileSystem.FileInfo.New("test.txt");
        fileToMove.MoveToEx("D:\\test.txt", false);

        Assert.True(_fileSystem.File.Exists("D:\\test.txt"));
        Assert.False(_fileSystem.File.Exists("test.txt"));
    }

    [Fact]
    public void CreateRandomHiddenTemporaryFile_DirectoryNotFound()
    {
        _fileSystem.Initialize();
        Assert.Throws<DirectoryNotFoundException>(() => _fileSystem.File.CreateRandomHiddenTemporaryFile("test"));
    }

    [Fact]
    public void CreateRandomHiddenTemporaryFile_UseUserTempWhenPathIsNull()
    {
        _fileSystem.Initialize();
        var fs = _fileSystem.File.CreateRandomHiddenTemporaryFile();
        var tmp = _fileSystem.Path.GetTempPath();
        Assert.StartsWith(tmp, fs.Name);

        Assert.True(_fileSystem.File.GetAttributes(fs.Name).HasFlag(FileAttributes.Hidden));

        fs.Write([1, 2, 3], 0, 3);
        fs.Position = 0;
        var output = new byte[3];
        fs.Read(output, 0, 3);
        Assert.Equal([1, 2, 3], output);
        fs.Dispose();
        Assert.False(_fileSystem.File.Exists(fs.Name));
    }

    [Fact]
    public void CreateRandomHiddenTemporaryFile()
    {
        _fileSystem.Initialize()
            .WithSubdirectory("test");

        var fs = _fileSystem.File.CreateRandomHiddenTemporaryFile("test");
        var tmp = _fileSystem.Path.GetFullPath("test");

        Assert.StartsWith(tmp, fs.Name);

        Assert.True(_fileSystem.File.GetAttributes(fs.Name).HasFlag(FileAttributes.Hidden));

        fs.Write([1, 2, 3], 0, 3);
        fs.Position = 0;
        var output = new byte[3];
        fs.Read(output, 0, 3);
        Assert.Equal([1, 2, 3], output);
        fs.Dispose();
        Assert.False(_fileSystem.File.Exists(fs.Name));
    }
}