using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="IFileInfo"/> class
/// </summary>
public static class FileExtensions
{
    /// <summary>
    /// Tries to copy an existing file. If the <paramref name="destination"/> already exists, it will be overwritten.
    /// </summary>
    /// <param name="source">The fil to copy.</param>
    /// <param name="destination">The copy target location.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    public static void CopyWithRetry(this IFileInfo source, string destination, int retryCount = 2, int retryDelay = 500)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));

        source.FileSystem.File.CopyWithRetry(source.FullName, destination, retryCount, retryDelay);
    }

    /// <summary>
    /// Tries to copy an existing file. If the <paramref name="destination"/> already exists, it will be overwritten.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="source">The fil to copy.</param>
    /// <param name="destination">The copy target location.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    public static void CopyWithRetry(this IFile _, string source, string destination, int retryCount = 2, int retryDelay = 500)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));

        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () =>
        {
            _.Copy(source, destination, true);
        });
    }


    /// <summary>
    /// Moves a file to a new location.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
    /// <param name="source">The file or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful; otherwise, <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists and <paramref name="overwrite"/>
    /// is <see langword="false"/>.</exception>
    /// <exception cref="FileNotFoundException">if the source was not found.</exception>
    public static bool MoveToEx(this IFileInfo source, string destination, bool overwrite)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));
        
        return MoveEx(source.FileSystem.File, source.FullName, destination, overwrite);
    }

    /// <summary>
    /// Moves a file to a new location.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
    /// <param name="_"></param>
    /// <param name="source">The file or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful; otherwise, <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists and <paramref name="overwrite"/>
    /// is <see langword="false"/>.</exception>
    /// <exception cref="FileNotFoundException">if the source was not found.</exception>
    public static bool MoveEx(this IFile _, string source, string destination, bool overwrite)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        _.FileSystem.File.Copy(source, destination, overwrite);

        try
        {
            _.FileSystem.File.Delete(source);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to move a file to a new location.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
    /// <param name="source">The file or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful; otherwise, <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
    /// <exception cref="FileNotFoundException">if the source was not found.</exception>
    public static bool MoveWithRetry(this IFileInfo source, string destination, bool overwrite = false, int retryCount = 2, int retryDelay = 500)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));
        return source.FileSystem.File.MoveWithRetry(source.FullName, destination, overwrite, retryCount, retryDelay);
    }

    /// <summary>
    /// Tries to move a file to a new location.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
    /// <param name="_"></param>
    /// <param name="source">The file or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful; otherwise, <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
    /// <exception cref="FileNotFoundException">if the source was not found.</exception>
    public static bool MoveWithRetry(this IFile _, string source, string destination, bool overwrite = false, int retryCount = 2, int retryDelay = 500)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));

        var sourceDeleted = false;
        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () =>
        {
            sourceDeleted = _.FileSystem.File.MoveEx(source, destination, overwrite);
        });
        return sourceDeleted;
    }

    /// <summary>
    /// Deletes a file if it's somewhere in the user's temporary directory.
    /// </summary>
    /// <param name="file">The file to get deleted.</param>
    public static void DeleteIfInTemp(this IFileInfo file)
    {
        if (file == null) 
            throw new ArgumentNullException(nameof(file));
        if (!file.Exists || !file.FileSystem.Path.IsChildOf(file.FileSystem.Path.GetTempPath(), file.FullName))
            return;
        file.Delete();
    }

    /// <summary>
    /// Deletes a file if it's somewhere in the user's temporary directory.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="file">The file to get deleted.</param>
    public static void DeleteIfInTemp(this IFile _, string file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
        if (!_.Exists(file)|| !_.FileSystem.Path.IsChildOf(_.FileSystem.Path.GetTempPath(), file))
            return;
        _.Delete(file);
    }

    /// <summary>
    /// Tries to delete a file. 
    /// </summary>
    /// <param name="file">The file to delete.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    public static void DeleteWithRetry(this IFileInfo file, int retryCount = 2, int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (file == null) 
            throw new ArgumentNullException(nameof(file));

        file.FileSystem.File.DeleteWithRetry(file.FullName, retryCount, retryDelay, errorAction);
    }

    /// <summary>
    /// Tries to delete a file. 
    /// </summary>
    /// <param name="_"></param>
    /// <param name="file">The file to delete.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    public static void DeleteWithRetry(this IFile _, string file, int retryCount = 2, int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (file == null) 
            throw new ArgumentNullException(nameof(file));

        if (!_.Exists(file))
            return;

        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () => _.Delete(file), errorAction: (ex, attempt) =>
        {
            if (ex is UnauthorizedAccessException)
            {
                if (attempt == 0)
                {
                    var attributes = _.GetAttributes(file);
                    if (attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        _.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                        errorAction?.Invoke(ex, attempt);
                        return true;
                    }
                }
            }
            errorAction?.Invoke(ex, attempt);
            return false;
        });
    }

    /// <summary>
    /// Tries to delete a file. 
    /// </summary>
    /// <param name="file">The file to delete.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="true"/> if the file is successfully deleted; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeleteWithRetry(this IFileInfo file, int retryCount = 2, int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        return file.FileSystem.File.TryDeleteWithRetry(file.FullName, retryCount, retryDelay, errorAction);
    }

    /// <summary>
    /// Tries to delete a file. 
    /// </summary>
    /// <param name="_"></param>
    /// <param name="file">The file to delete.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="true"/> if the file is successfully deleted; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeleteWithRetry(this IFile _, string file, int retryCount = 2, int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        if (!_.Exists(file))
            return true;

        return FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () => _.Delete(file), false,
            (ex, attempt) =>
            {
                if (ex is UnauthorizedAccessException)
                {
                    if (attempt == 0)
                    {
                        var attributes = _.GetAttributes(file);
                        if (attributes.HasFlag(FileAttributes.ReadOnly))
                        {
                            _.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                            errorAction?.Invoke(ex, attempt);
                            return true;
                        }
                    }
                }

                errorAction?.Invoke(ex, attempt);
                return false;
            });
    }

    /// <summary>
    /// Creates a temporary, hidden file which gets deleted once the returned stream is disposed.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="directory">The directory where the temporary file shall be created.
    /// If <paramref name="directory"/> is <see langword="null"/> the current user's temporary directory will be used.</param>
    /// <returns>A temporary file which gets deleted on disposal.</returns>
    /// <exception cref="DirectoryNotFoundException"><paramref name="directory"/> is not found.</exception>
    public static FileSystemStream CreateRandomHiddenTemporaryFile(this IFile _, string? directory = null)
    {
        var fs = _.FileSystem;

        directory = directory is null ? fs.Path.GetTempPath() : fs.Path.GetFullPath(directory);

        if (!fs.Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Could not find the target directory '{directory}'");

        FileSystemStream stream = null!;
        FileSystemUtilities.ExecuteFileSystemActionWithRetry(3, 500, () =>
        {
            var randomName = fs.Path.GetRandomFileName();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                randomName = "." + randomName;

            var tempFilePath = fs.Path.GetFullPath(fs.Path.Combine(directory, randomName));
            stream = fs.FileStream.New(tempFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 0x1000, FileOptions.DeleteOnClose);
            fs.File.SetAttributes(tempFilePath, FileAttributes.Temporary | FileAttributes.Hidden);
        });
        return stream;
    }
}