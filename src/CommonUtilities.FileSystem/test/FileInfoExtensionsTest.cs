using System.IO;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class FileInfoExtensionsTest
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void Test_DeleteIfInTemp()
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
    public void Test_DeleteWithRetry()
    {
        _fileSystem.Initialize()
            .WithFile("text1.txt")
            .WithFile("text2.txt");

        var file1 = _fileSystem.FileInfo.New("text1.txt");
        var file2 = _fileSystem.FileInfo.New("text2.txt");
        file2.Attributes |= FileAttributes.ReadOnly;

        file1.Refresh();
        file2.Refresh();
        file1.DeleteWithRetry();
        file2.DeleteWithRetry();
        
        file1.Refresh();
        file2.Refresh();
        Assert.False(file1.Exists);
        Assert.False(file2.Exists);
    }

    [Fact]
    public void Test_CopyWithRetry_ThrowsFileNotFound()
    {
        _fileSystem.Initialize();
        var fileToCopy = _fileSystem.FileInfo.New("test.txt");
        Assert.Throws<FileNotFoundException>(() => fileToCopy.CopyWithRetry("test1.txt"));
    }

    [Fact]
    public void Test_CopyWithRetry()
    {
        _fileSystem.Initialize().WithFile("test.txt").Which(f => f.HasStringContent("1234"));
        _fileSystem.Initialize().WithFile("test1.txt");

        var fi = _fileSystem.FileInfo.New("test.txt");
        fi.CopyWithRetry("test1.txt");
        Assert.Equal("1234", _fileSystem.File.ReadAllText("test1.txt"));
        Assert.True(_fileSystem.File.Exists("test.txt"));
    }


    [Fact]
    public void Test_MoveTo_ThrowsFileNotFoundException()
    {
        var fileToMove = _fileSystem.FileInfo.New("test.txt");
        Assert.Throws<FileNotFoundException>(() => fileToMove.MoveTo("test.txt", false));
    }


    [Fact]
    public void Test_MoveTo_NoOverwrite_ThrowsIOException()
    {
        _fileSystem.Initialize().WithFile("test.txt").Which(f => f.HasStringContent("1234"));
        _fileSystem.Initialize().WithFile("test1.txt");

        var fileToMove = _fileSystem.FileInfo.New("test.txt");
        Assert.Throws<IOException>(() => fileToMove.MoveTo("test1.txt", false));
    }

    [Fact]
    public void Test_MoveTo_WithOverwrite()
    {

        _fileSystem.Initialize().WithFile("test.txt").Which(f => f.HasStringContent("1234"));
        _fileSystem.Initialize().WithFile("test1.txt");

        var fileToMove = _fileSystem.FileInfo.New("test.txt");

        fileToMove.MoveTo("test1.txt", true);
        Assert.Equal("1234", _fileSystem.File.ReadAllText("test1.txt"));
        Assert.False(_fileSystem.File.Exists("test.txt"));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Test_MoveTo_AcrossVolume_Windows()
    {
        _fileSystem.WithDrive("D:");
        _fileSystem.Initialize().WithFile("test.txt").Which(f => f.HasStringContent("1234"));
        _fileSystem.Initialize().WithFile("test1.txt");

        var fileToMove = _fileSystem.FileInfo.New("test.txt");
        fileToMove.MoveTo("D:\\test.txt", false);

        Assert.True(_fileSystem.File.Exists("D:\\test.txt"));
        Assert.False(_fileSystem.File.Exists("test.txt"));
    }
}