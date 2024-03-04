using System.IO;
using System.IO.Abstractions;
using Validation;

namespace AnakinRaW.CommonUtilities.FileSystem;

public static class FileSystemExtensions
{
    public static FileSystemStream? CreateFileWithRetry(this IFileSystem fs, string path, int retryCount = 2, int retryDelay = 500)
    {
        Requires.NotNullOrEmpty(path, nameof(path));
        FileSystemStream? stream = null;
        ExecuteFileActionWithRetry(retryCount, retryDelay,
            () => stream = fs.FileStream.New(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None));
        return stream;
    }

    public static IDirectoryInfo? CreateTemporaryFolderInTempWithRetry(this IFileSystem fs, int retryCount = 2, int retryDelay = 500)
    {
        IDirectoryInfo? newTempFolder = null;
        ExecuteFileActionWithRetry(retryCount, retryDelay, () =>
        {
            var tempFolder = fs.Path.GetTempPath();
            var folderName = fs.Path.GetRandomFileName();
            var fullFolderPath = fs.Path.Combine(tempFolder, folderName);
            newTempFolder = fs.Directory.CreateDirectory(fullFolderPath);
        });
        return newTempFolder;
    }
}