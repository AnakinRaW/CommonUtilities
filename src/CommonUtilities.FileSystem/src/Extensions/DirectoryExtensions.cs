using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="IDirectoryInfo"/> class
/// </summary>
public static class DirectoryExtensions
{
    /// <summary>
    /// Moves a directory to a new location. In contrast to <see cref="IDirectoryInfo.MoveTo"/>, this method works across drives.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails.</remarks>
    /// <param name="source">The file or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful;
    /// <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.</exception>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    public static bool MoveToEx(this IDirectoryInfo source, string destination, IProgress<double>? progress, DirectoryOverwriteOption overwrite)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        if (!source.Exists)
            throw new DirectoryNotFoundException();

        var fs = source.FileSystem;

        if (fs.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                fs.Directory.Delete(destination, true);
        }
        var destinationFullPath = fs.Path.GetFullPath(destination);
        if (source.FullName.Equals(destinationFullPath))
            return false;
        progress?.Report(0.0);
        new DirectoryCopier(fs, progress).CopyDirectory(source, destination, true);
        var deleteSuccess = source.DeleteWithRetry();
        progress?.Report(1.0);
        return deleteSuccess;
    }

    /// <summary>
    /// Tries to move a directory to a new location. In contrast to <see cref="IDirectoryInfo.MoveTo"/>, this method works across drives.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.</exception>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    public static void MoveToWithRetry(this IDirectoryInfo source, string destination,
        DirectoryOverwriteOption overwrite = DirectoryOverwriteOption.NoOverwrite,
        int retryCount = 2, int retryDelay = 500)
    {
        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () => source.MoveToEx(destination, null, overwrite));
    }

    /// <summary>
    /// Movies a directory asynchronously. In contrast to <see cref="IDirectoryInfo.MoveTo"/>, this method works across drives.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="workerCount">Number of parallel workers copying the directory.
    /// If worker count shall be 1 consider using <see cref="MoveToEx"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful;
    /// <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.</exception>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    public static async Task<bool> MoveToAsync(this IDirectoryInfo source, string destination, IProgress<double>? progress,
        DirectoryOverwriteOption overwrite, int workerCount = 2, CancellationToken cancellationToken = default)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));

        var fs = source.FileSystem;

        cancellationToken.ThrowIfCancellationRequested();
        if (!source.Exists)
            throw new DirectoryNotFoundException($"Directory '{source.FullName}' not found");
        if (fs.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                await Task.Run(() => fs.Directory.Delete(destination, true), cancellationToken);
        }

        var destinationFullPath = fs.Path.GetFullPath(destination);
        if (source.FullName.Equals(destinationFullPath))
            return false;
        progress?.Report(0.0);
        await new DirectoryCopier(fs, progress, workerCount).CopyDirectoryAsync(source, destination, true, cancellationToken);
        var deleteSuccess = DeleteWithRetry(source);
        progress?.Report(1.0);
        return deleteSuccess;
    }

    /// <summary>
    /// Copies a directory to a different location.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    public static void Copy(this IDirectoryInfo source, string destination, IProgress<double>? progress, DirectoryOverwriteOption overwrite)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));

        var fs = source.FileSystem;

        if (!source.Exists)
            throw new DirectoryNotFoundException($"Directory '{source.FullName}' not found");
        if (fs.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                fs.Directory.Delete(destination, true);
        }
        var destinationFullPath = fs.Path.GetFullPath(destination);
        if (source.FullName.Equals(destinationFullPath))
            return;
        progress?.Report(0.0);
        new DirectoryCopier(fs, progress).CopyDirectory(source, destination, false);
        progress?.Report(1.0);
    }

    /// <summary>
    ///  Tries to copy a directory to a new location.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.</exception>
    public static void CopyWithRetry(this IDirectoryInfo source, string destination,
        DirectoryOverwriteOption overwrite = DirectoryOverwriteOption.NoOverwrite,
        int retryCount = 2, int retryDelay = 500)
    {
        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay,
            () => source.Copy(destination, null, overwrite));
    }

    /// <summary>
    /// Copies a directory asynchronously.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="workerCount">Number of parallel workers copying the directory.
    /// If worker count shall be 1 consider using <see cref="Copy"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The task of the operation.</returns>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.</exception>
    public static async Task CopyAsync(this IDirectoryInfo source, string destination, IProgress<double>? progress,
        DirectoryOverwriteOption overwrite, int workerCount = 2, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var fs = source.FileSystem;

        cancellationToken.ThrowIfCancellationRequested();
        if (!source.Exists)
            throw new DirectoryNotFoundException($"Directory '{source.FullName}' not found");
        if (fs.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                await Task.Run(() => fs.Directory.Delete(destination, true), cancellationToken);
        }
        var destinationFullPath = fs.Path.GetFullPath(destination);
        if (source.FullName.Equals(destinationFullPath))
            return;
        progress?.Report(0.0);
        await new DirectoryCopier(fs, progress, workerCount).CopyDirectoryAsync(source, destination, false,
            cancellationToken);
        progress?.Report(1.0);
    }

    /// <summary>
    /// Tries to delete a directory. 
    /// </summary>
    /// <param name="directory">The file to delete.</param>
    /// <param name="recursive">When <see langword="true"/> all contents of the directory get deleted.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="false"/> if the operation failed. <see langword="true"/> otherwise.</returns>
    public static bool DeleteWithRetry(this IDirectoryInfo directory, bool recursive = true, int retryCount = 2,
        int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        return !directory.Exists || FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay,
            () => directory.Delete(recursive), errorAction: errorAction);
    }
}