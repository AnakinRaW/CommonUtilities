using System;
using System.IO;
using System.IO.Abstractions;

namespace AnakinRaW.CommonUtilities.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="IFileSystem"/> class
/// </summary>
public static class FileSystemExtensions
{
    /// <summary>
    /// Tries to create a new file and returns an open <see cref="FileSystemStream"/> to the created file, or <see langword="null"/> if the file could not be created.
    /// An existing file will be overwritten.
    /// </summary>
    /// <param name="fs"></param>
    /// <param name="path">The file's location.</param>
    /// <param name="fileAccess">A bitwise combination of the enumeration values that determines how the file can be accessed by the <see cref="FileSystemStream"/> object.</param>
    /// <param name="fileShare">A bitwise combination of the enumeration values that determines how the file will be shared by processes.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <returns>Open file stream or <see langword="null"/> if the file could not be created.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fs"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    public static FileSystemStream? CreateFileWithRetry(
        this IFileSystem fs, 
        string path, 
        FileAccess fileAccess = FileAccess.ReadWrite,
        FileShare fileShare = FileShare.None,
        int retryCount = 2,
        int retryDelay = 500)
    {
        if (fs == null) 
            throw new ArgumentNullException(nameof(fs));
        if (path == null) 
            throw new ArgumentNullException(nameof(path));

        FileSystemStream? stream = null;
        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay,
            () => stream = fs.FileStream.New(path, FileMode.Create, fileAccess, fileShare));
        return stream;
    }

    /// <summary>
    /// Tries to create a new unique folder within the current users temporary directory.
    /// </summary>
    /// <param name="fs"></param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <returns>The <see cref="IDirectoryInfo"/> of the created folder or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fs"/> is <see langword="null"/>.</exception>
    public static IDirectoryInfo? CreateTemporaryFolderInTempWithRetry(this IFileSystem fs, int retryCount = 2, int retryDelay = 500)
    {
        if (fs == null) 
            throw new ArgumentNullException(nameof(fs));

        IDirectoryInfo? newTempFolder = null;
        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () =>
        {
            var tempFolder = fs.Path.GetTempPath();
            var folderName = fs.Path.GetRandomFileName();
            var fullFolderPath = fs.Path.Combine(tempFolder, folderName);
            newTempFolder = fs.Directory.CreateDirectory(fullFolderPath);
        });
        return newTempFolder;
    }
}