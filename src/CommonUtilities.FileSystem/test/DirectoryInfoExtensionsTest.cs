using System;
using System.IO;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class DirectoryInfoExtensionsTest
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void Test_DeleteWithRetry()
    {
        _fileSystem.Initialize()
            .WithFile("test/text1.txt")
            .WithSubdirectory("test1");

        var dir1 = _fileSystem.DirectoryInfo.New("test");
        var dir2 = _fileSystem.DirectoryInfo.New("test1");

        Assert.Throws<IOException>(() => dir1.DeleteWithRetry(false));
        Assert.Throws<IOException>(() => _fileSystem.Directory.DeleteWithRetry(dir1.FullName, false));

        // https://github.com/Testably/Testably.Abstractions/issues/549
        //var fs = _fileSystem.FileStream.New("test/text1.txt", FileMode.Open);
        //Assert.Throws<IOException>(() => dir1.DeleteWithRetry());
        //Assert.Throws<IOException>(() => _fileSystem.Directory.DeleteWithRetry(dir1.FullName));

        //fs.Dispose();

        dir1.DeleteWithRetry();
        _fileSystem.Directory.DeleteWithRetry(dir2.FullName);

        dir1.Refresh();
        dir2.Refresh();

        Assert.False(dir1.Exists);
        Assert.False(dir2.Exists);
    }

    [Fact]
    public void Test_TryDeleteWithRetry()
    {
        _fileSystem.Initialize()
            .WithFile("test/text1.txt")
            .WithSubdirectory("test1");

        var dir1 = _fileSystem.DirectoryInfo.New("test");
        var dir2 = _fileSystem.DirectoryInfo.New("test1");

        Assert.False(dir1.TryDeleteWithRetry(false));
        Assert.False(_fileSystem.Directory.TryDeleteWithRetry(dir1.FullName, false));

        var fs = _fileSystem.FileStream.New("test/text1.txt", FileMode.Open);
        Assert.False(dir1.TryDeleteWithRetry(false));
        Assert.False(_fileSystem.Directory.TryDeleteWithRetry(dir1.FullName, false));

        fs.Dispose();
        dir1.TryDeleteWithRetry();
        _fileSystem.Directory.TryDeleteWithRetry(dir2.FullName);

        Assert.True(dir1.TryDeleteWithRetry());
        Assert.True(_fileSystem.Directory.TryDeleteWithRetry(dir2.FullName));

        dir1.Refresh();
        dir2.Refresh();

        Assert.True(!dir1.Exists);
        Assert.True(!dir2.Exists);
    }

    [Fact]
    public void Test_MoveToEx_ThrowsDirectoryNotFound()
    {
        _fileSystem.Initialize();
        var dirToMove = _fileSystem.DirectoryInfo.New("test");
        Assert.Throws<DirectoryNotFoundException>(() => dirToMove.MoveToEx("test1", null, DirectoryOverwriteOption.NoOverwrite));
    }

    [Fact]
    public void Test_MoveToEx_NoOverwrite_ThrowsIOException()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithSubdirectory("other");

        var dirToMove = _fileSystem.DirectoryInfo.New("test");
        Assert.Throws<IOException>(() => dirToMove.MoveToEx("other", null, DirectoryOverwriteOption.NoOverwrite));
    }


    [Fact]
    public void Test_MoveToEx_CleanOverride()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");


        var delSuc = dirToMove.MoveToEx("other", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("other"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("other").GetFiles("*").Length);
    }

    [Fact]
    public void Test_MoveToEx_MergeOverride()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");


        var delSuc = dirToMove.MoveToEx("other", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("other"));
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("other").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Test_MoveToEx_MoveAcrossVolumes()
    {
        _fileSystem.WithDrive("D:");
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");

        dirToMove.MoveToEx("D:\\test", null, DirectoryOverwriteOption.NoOverwrite);
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("D:\\test").GetFiles("*").Length);
    }

    [Fact]
    public void Test_MoveToEx_WithProgress()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");


        var progressValue = 0d;
        var progress = new BlockingProgress(d =>
        {
            progressValue = d;
        });
        var delSuc = dirToMove.MoveToEx("other", progress, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(delSuc);
        Assert.Equal(1.0, progressValue);
    }

    [Fact(Skip = "https://github.com/Testably/Testably.Abstractions/issues/549")]
    public void Test_MoveToEx_CannotDeleteSource()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");

        var fs = _fileSystem.FileStream.New("test/1.txt", FileMode.Open, FileAccess.Read, FileShare.Read);

        var delSuc = dirToMove.MoveToEx("other", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.False(delSuc);

        fs.Dispose();
    }


    [Fact]
    public async void Test_MoveToAsync_ThrowsDirectoryNotFound()
    {
        _fileSystem.Initialize();
        var dirToMove = _fileSystem.DirectoryInfo.New("test");
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await dirToMove.MoveToAsync("test1", null, DirectoryOverwriteOption.NoOverwrite));
    }

    [Fact]
    public async void Test_MoveToAsync_NoOverwrite_ThrowsIOException()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithSubdirectory("other");

        var dirToMove = _fileSystem.DirectoryInfo.New("test");
        await Assert.ThrowsAsync<IOException>(async () => await dirToMove.MoveToAsync("other", null, DirectoryOverwriteOption.NoOverwrite));
    }


    [Fact]
    public async void Test_MoveToAsync_CleanOverride()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");


        var delSuc = await dirToMove.MoveToAsync("other", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("other"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("other").GetFiles("*").Length);
    }

    [Fact]
    public async void Test_MoveToAsync_MergeOverride()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");


        var delSuc = await dirToMove.MoveToAsync("other", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("other"));
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("other").GetFiles("*").Length);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async void Test_MoveToAsync_MoveAcrossVolumes()
    {
        _fileSystem.WithDrive("D:");
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");

        await dirToMove.MoveToAsync("D:\\test", null, DirectoryOverwriteOption.NoOverwrite);
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("D:\\test").GetFiles("*").Length);
    }

    [Fact]
    public async void Test_MoveToAsync_WithProgress()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");

        var progressValue = 0d;
        var progress = new BlockingProgress(d =>
        {
            progressValue = d;
        });
        var delSuc = await dirToMove.MoveToAsync("other", progress, DirectoryOverwriteOption.CleanOverwrite);
        
        Assert.True(delSuc);
        Assert.Equal(1.0, progressValue);
    }

    [Fact(Skip = "https://github.com/Testably/Testably.Abstractions/issues/549")]
    public async void Test_MoveToAsync_CannotDeleteSource()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToMove = _fileSystem.DirectoryInfo.New("test");

        var fs = _fileSystem.FileStream.New("test/1.txt", FileMode.Open, FileAccess.Read, FileShare.Read);

        var delSuc = await dirToMove.MoveToAsync("other", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.False(delSuc);

        fs.Dispose();
    }

    [Fact]
    public void Test_Copy_ThrowsDirectoryNotFound()
    {
        var dirToCopy = _fileSystem.DirectoryInfo.New("test");
        Assert.Throws<DirectoryNotFoundException>(() => dirToCopy.Copy("test1", null, DirectoryOverwriteOption.NoOverwrite));
    }

    [Fact]
    public void Test_Copy_NoOverwrite_ThrowsIOException()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToCopy = _fileSystem.DirectoryInfo.New("test");

        Assert.Throws<IOException>(() => dirToCopy.Copy("other", null, DirectoryOverwriteOption.NoOverwrite));
    }

    [Fact]
    public void Test_Copy_CleanOverwrite()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToCopy = _fileSystem.DirectoryInfo.New("test");

        dirToCopy.Copy("other", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("other"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("other").GetFiles("*").Length);
    }

    [Fact]
    public void Test_Copy_MergeOverwrite()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToCopy = _fileSystem.DirectoryInfo.New("test");

        dirToCopy.Copy("other", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.True(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("other"));
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("other").GetFiles("*").Length);
    }


    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Test_Copy_AcrossDrives()
    {
        _fileSystem.WithDrive("D:");
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"));

        var dirToCopy = _fileSystem.DirectoryInfo.New("test");

        dirToCopy.Copy("D:\\other", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("D:\\other"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("D:\\other").GetFiles("*").Length);
    }

    [Fact]
    public async void Test_CopyAsync_ThrowsDirectoryNotFound()
    {
        var dirToCopy = _fileSystem.DirectoryInfo.New("test");
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await dirToCopy.CopyAsync("test1", null, DirectoryOverwriteOption.NoOverwrite));
    }

    [Fact]
    public async void Test_CopyAsync_NoOverwrite_ThrowsIOException()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToCopy = _fileSystem.DirectoryInfo.New("test");

        await Assert.ThrowsAsync<IOException>(async () => await dirToCopy.CopyAsync("other", null, DirectoryOverwriteOption.NoOverwrite));
    }

    [Fact]
    public async void Test_CopyAsync_CleanOverwrite()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToCopy = _fileSystem.DirectoryInfo.New("test");

        await dirToCopy.CopyAsync("other", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("other"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("other").GetFiles("*").Length);
    }

    [Fact]
    public async void Test_CopyAsync_MergeOverwrite()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("3"));

        var dirToCopy = _fileSystem.DirectoryInfo.New("test");

        await dirToCopy.CopyAsync("other", null, DirectoryOverwriteOption.MergeOverwrite);
        Assert.True(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("other"));
        Assert.Equal(3, _fileSystem.DirectoryInfo.New("other").GetFiles("*").Length);
    }


    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async void Test_CopyAsync_AcrossDrives()
    {
        _fileSystem.WithDrive("D:");
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"));

        var dirToCopy = _fileSystem.DirectoryInfo.New("test");

        await dirToCopy.CopyAsync("D:\\other", null, DirectoryOverwriteOption.CleanOverwrite);
        Assert.True(_fileSystem.Directory.Exists("test"));
        Assert.True(_fileSystem.Directory.Exists("D:\\other"));
        Assert.Equal(2, _fileSystem.DirectoryInfo.New("D:\\other").GetFiles("*").Length);
    }


    /// <summary>
    /// A progress which handles get invoked on the current thread instead of a captured synchronization context.
    /// </summary>
    private class BlockingProgress(Action<double> handler) : IProgress<double>
    {
        public void Report(double value)
        {
            handler(value);
        }
    }
}