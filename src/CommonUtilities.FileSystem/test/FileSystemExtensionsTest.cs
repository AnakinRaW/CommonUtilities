using System;
using System.Runtime.InteropServices;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class FileSystemExtensionsTest
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void CreateTemporaryFolderInTempWithRetry()
    {
        _fileSystem.Initialize();
        var dir = _fileSystem.CreateTemporaryFolderInTempWithRetry();
        var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        Assert.StartsWith(_fileSystem.Path.GetTempPath(), dir!.FullName, comparer);
    }


    [Fact]
    public void CreateFileWithRetry()
    {
        _fileSystem.Initialize().WithFile("test.txt")
            .Which(a => a.HasStringContent("test"));
        var fs = _fileSystem.CreateFileWithRetry("test.txt");
        Assert.NotNull(fs);
        fs.Dispose();
        // Assert file is overwritten.
        Assert.Equal(string.Empty, _fileSystem.File.ReadAllText("test.txt"));


        fs = _fileSystem.CreateFileWithRetry("test2.txt");
        Assert.NotNull(fs);
        Assert.True(_fileSystem.File.Exists("test2.txt"));
        fs.Dispose();
    }
}