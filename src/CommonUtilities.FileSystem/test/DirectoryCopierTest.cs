using System;
using System.IO;
using System.Threading.Tasks;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class DirectoryCopierTest
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void Test_Ctor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DirectoryCopier(null!));
    }
    
    [Fact]
    public void Test_ThrowsArgumentNullExceptions()
    {
        var copier = new DirectoryCopier(_fileSystem);
        Assert.Throws<ArgumentNullException>(() => copier.MoveDirectory(null!, "path"));
        Assert.Throws<ArgumentNullException>(() => copier.MoveDirectory("source", null!));
        Assert.Throws<ArgumentNullException>(() => copier.CopyDirectory(null!, "path"));
        Assert.Throws<ArgumentNullException>(() => copier.CopyDirectory("source", null!));
    }

    [Fact]
    public async Task Test_ThrowsArgumentNullExceptionsAsync()
    {
        var copier = new DirectoryCopier(_fileSystem);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await copier.MoveDirectoryAsync(null!, "path"));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await copier.MoveDirectoryAsync("source", null!));
        await Assert.ThrowsAsync<ArgumentException>(async () => await copier.MoveDirectoryAsync("source", "path", null, null, 0));
        await Assert.ThrowsAsync<ArgumentException>(async () => await copier.MoveDirectoryAsync("source", "path", null, null, -1));

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await copier.CopyDirectoryAsync(null!, "path"));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await copier.CopyDirectoryAsync("source", null!));
        await Assert.ThrowsAsync<ArgumentException>(async () => await copier.CopyDirectoryAsync("source", "path", null, null, 0));
        await Assert.ThrowsAsync<ArgumentException>(async () => await copier.CopyDirectoryAsync("source", "path", null, null, -1));
    }

    [Fact]
    public void Test_CopyDirectory()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("test/3.txt").Which(f => f.HasStringContent("3"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("99"));

        var copier = new DirectoryCopier(_fileSystem);

        var progressValue = 0d;
        var progress = new BlockingProgress(d =>
        {
            progressValue = d;
        });

        // https://github.com/Testably/Testably.Abstractions/issues/549
        //var fsStream = _fileSystem.FileStream.New("test/1.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        copier.CopyDirectory("test", "other", progress, Exclude2);
        Assert.Equal(1.0, progressValue);

        Assert.True(_fileSystem.File.Exists("other/1.txt"));
        Assert.Equal("3", _fileSystem.File.ReadAllText("other/3.txt"));

        //fsStream.Dispose();

        return;


        bool Exclude2(string s)
        {
            return _fileSystem.Path.GetFileName(s) != "2.txt";
        }
    }

    [Fact]
    public async Task Test_CopyDirectoryAsync()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("test/3.txt").Which(f => f.HasStringContent("3"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("99"));

        var copier = new DirectoryCopier(_fileSystem);

        var progressValue = 0d;
        var progress = new BlockingProgress(d =>
        {
            progressValue = d;
        });

        // https://github.com/Testably/Testably.Abstractions/issues/549
        //var fsStream = _fileSystem.FileStream.New("test/1.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        await copier.CopyDirectoryAsync("test", "other", progress, Exclude2);
        Assert.Equal(1.0, progressValue);

        Assert.True(_fileSystem.File.Exists("other/1.txt"));
        Assert.Equal("3", _fileSystem.File.ReadAllText("other/3.txt"));

        //fsStream.Dispose();

        return;

        bool Exclude2(string s)
        {
            return _fileSystem.Path.GetFileName(s) != "2.txt";
        }
    }

    [Fact]
    public void Test_MoveDirectory()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("test/3.txt").Which(f => f.HasStringContent("3"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("99"));

        var copier = new DirectoryCopier(_fileSystem);

        var progressValue = 0d;
        var progress = new BlockingProgress(d =>
        {
            progressValue = d;
        });

        var delSuc = copier.MoveDirectory("test", "other", progress, Exclude2);
        Assert.Equal(1.0, progressValue);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("test"));

        var destinationFiles = _fileSystem.Directory.GetFiles("other");
        Assert.Equal(2, destinationFiles.Length);
        Assert.True(_fileSystem.File.Exists("other/1.txt"));
        Assert.Equal("3", _fileSystem.File.ReadAllText("other/3.txt"));
        return;


        bool Exclude2(string s)
        {
            return _fileSystem.Path.GetFileName(s) != "2.txt";
        }
    }

    [Fact(Skip = "https://github.com/Testably/Testably.Abstractions/issues/549")]
    public void Test_MoveDirectory_CannotDeleteSource()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("test/3.txt").Which(f => f.HasStringContent("3"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("99"));

        var copier = new DirectoryCopier(_fileSystem);

        var progressValue = 0d;
        var progress = new BlockingProgress(d =>
        {
            progressValue = d;
        });

        var fs = _fileSystem.FileStream.New("test/1.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var delSuc = copier.MoveDirectory("test", "other", progress);
        Assert.Equal(1.0, progressValue);
        Assert.False(delSuc);
        Assert.True(_fileSystem.Directory.Exists("test"));

        Assert.True(_fileSystem.File.Exists("other/1.txt"));
        Assert.True(_fileSystem.File.Exists("other/2.txt"));
        Assert.Equal("3", _fileSystem.File.ReadAllText("other/3.txt"));

        fs.Dispose();
    }

    [Fact]
    public async Task Test_MoveDirectoryAsync()
    {
        _fileSystem.Initialize()
            .WithFile("test/1.txt").Which(f => f.HasStringContent("1"))
            .WithFile("test/2.txt").Which(f => f.HasStringContent("2"))
            .WithFile("test/3.txt").Which(f => f.HasStringContent("3"))
            .WithFile("other/3.txt").Which(f => f.HasStringContent("99"));

        var copier = new DirectoryCopier(_fileSystem);

        var progressValue = 0d;
        var progress = new BlockingProgress(d =>
        {
            progressValue = d;
        });

        
        var delSuc = await copier.MoveDirectoryAsync("test", "other", progress);
        Assert.Equal(1.0, progressValue);
        Assert.True(delSuc);
        Assert.False(_fileSystem.Directory.Exists("test"));

        Assert.True(_fileSystem.File.Exists("other/1.txt"));
        Assert.True(_fileSystem.File.Exists("other/2.txt"));
        Assert.Equal("3", _fileSystem.File.ReadAllText("other/3.txt"));
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