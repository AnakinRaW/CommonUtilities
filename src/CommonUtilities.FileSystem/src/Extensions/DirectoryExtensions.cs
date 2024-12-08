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
    /// <param name="source">The directory or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful; otherwise, <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.</exception>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    public static bool MoveToEx(this IDirectoryInfo source, string destination, IProgress<double>? progress, DirectoryOverwriteOption overwrite)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));
        return source.FileSystem.Directory.MoveToEx(source.FullName, destination, progress, overwrite);
    }

    /// <summary>
    /// Moves a directory to a new location. In contrast to <see cref="IDirectoryInfo.MoveTo"/>, this method works across drives.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails.</remarks>
    /// <param name="_"></param>
    /// <param name="source">The directory or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful; otherwise, <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.</exception>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    public static bool MoveToEx(this IDirectory _, string source, string destination, IProgress<double>? progress, DirectoryOverwriteOption overwrite)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        var fs = _.FileSystem;

        if (fs.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                fs.Directory.Delete(destination, true);
        }

        return new DirectoryCopier(fs).MoveDirectory(source, destination, progress);
    }

    /// <summary>
    /// Movies a directory asynchronously. In contrast to <see cref="IDirectoryInfo.MoveTo"/>, this method works across drives.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="concurrentWorkers">Number of parallel workers copying the directory.
    /// If there shall be only 1 worker, consider using <see cref="MoveToEx(IDirectoryInfo,string,System.IProgress{double}?,DirectoryOverwriteOption)"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful; otherwise, <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.</exception>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    public static Task<bool> MoveToAsync(this IDirectoryInfo source, string destination, IProgress<double>? progress,
        DirectoryOverwriteOption overwrite, int concurrentWorkers = 2, CancellationToken cancellationToken = default)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));

        return source.FileSystem.Directory.MoveToAsync(source.FullName, destination, progress, overwrite, concurrentWorkers, cancellationToken);
    }

    /// <summary>
    /// Movies a directory asynchronously. In contrast to <see cref="IDirectoryInfo.MoveTo"/>, this method works across drives.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="concurrentWorkers">Number of parallel workers copying the directory.
    /// If there shall be only 1 worker, consider using <see cref="MoveToEx(IDirectoryInfo,string,System.IProgress{double}?,DirectoryOverwriteOption)"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langowrd="true"/> if the deletion of the source was successful; otherwise, <see langowrd="false"/> if source was not deleted.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.</exception>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    public static async Task<bool> MoveToAsync(this IDirectory _, string source, string destination, IProgress<double>? progress,
        DirectoryOverwriteOption overwrite, int concurrentWorkers = 2, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));

        var fs = _.FileSystem;
        
        if (fs.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                await Task.Run(() => fs.Directory.Delete(destination, true), cancellationToken).ConfigureAwait(false);
        }

        return await new DirectoryCopier(fs)
            .MoveDirectoryAsync(source, destination, progress, null, concurrentWorkers, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Copies a directory to a different location.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    public static void Copy(this IDirectoryInfo source, string destination, IProgress<double>? progress, DirectoryOverwriteOption overwrite)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));

        source.FileSystem.Directory.Copy(source.FullName, destination, progress, overwrite);
    }

    /// <summary>
    /// Copies a directory to a different location.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    public static void Copy(this IDirectory _, string source, string destination, IProgress<double>? progress, DirectoryOverwriteOption overwrite)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));

        var fs = _.FileSystem;

        if (fs.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                fs.Directory.Delete(destination, true);
        }

        new DirectoryCopier(fs).CopyDirectory(source, destination, progress);
    }

    /// <summary>
    /// Copies a directory asynchronously.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="workerCount">Number of parallel workers copying the directory.
    /// If there shall be only 1 worker, consider using <see cref="Copy(IDirectoryInfo,string,System.IProgress{double}?,DirectoryOverwriteOption)"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The task of the operation.</returns>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="IOException">
    /// If <paramref name="destination"/> already exists and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    public static Task CopyAsync(this IDirectoryInfo source, string destination, IProgress<double>? progress,
        DirectoryOverwriteOption overwrite, int workerCount = 2, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        return source.FileSystem.Directory.CopyAsync(source.FullName, destination, progress, overwrite, workerCount,
            cancellationToken);
    }

    /// <summary>
    /// Copies a directory asynchronously.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="workerCount">Number of parallel workers copying the directory.
    /// If there shall be only 1 worker, consider using <see cref="Copy(IDirectoryInfo,string,System.IProgress{double}?,DirectoryOverwriteOption)"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The task of the operation.</returns>
    /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
    /// <exception cref="IOException">
    /// If <paramref name="destination"/> already exists and <paramref name="overwrite"/> is <see cref="DirectoryOverwriteOption.NoOverwrite"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    public static async Task CopyAsync(this IDirectory _, string source, string destination, IProgress<double>? progress,
        DirectoryOverwriteOption overwrite, int workerCount = 2, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null) 
            throw new ArgumentNullException(nameof(destination));

        var fs = _.FileSystem;

        if (fs.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                await Task.Run(() => fs.Directory.Delete(destination, true), cancellationToken).ConfigureAwait(false);
        }

        await new DirectoryCopier(fs)
            .CopyDirectoryAsync(source, destination, progress, null, workerCount, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Tries to delete a directory. 
    /// </summary>
    /// <param name="directory">The directory to delete.</param>
    /// <param name="recursive">When <see langword="true"/> all contents of the directory get deleted.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="true"/> if the directory is deleted; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
    public static void DeleteWithRetry(this IDirectoryInfo directory, bool recursive = true, int retryCount = 2,
        int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (directory == null) 
            throw new ArgumentNullException(nameof(directory));

        directory.FileSystem.Directory.DeleteWithRetry(directory.FullName, recursive, retryCount, retryDelay, errorAction);
    }

    /// <summary>
    /// Deletes a specified directory. 
    /// </summary>
    /// <param name="_"></param>
    /// <param name="directory">The directory to delete.</param>
    /// <param name="recursive">When <see langword="true"/> all contents of the directory get deleted.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="false"/> if the operation failed; otherwise, <see langword="true"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
    public static void DeleteWithRetry(this IDirectory _, string directory, bool recursive = true, int retryCount = 2,
        int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (directory == null)
            throw new ArgumentNullException(nameof(directory));

        if (!_.Exists(directory))
            return;

        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay,
            () => _.Delete(directory, recursive), errorAction: errorAction);
    }

    /// <summary>
    /// Tries to delete a specified directory. 
    /// </summary>
    /// <param name="directory">The specified to delete.</param>
    /// <param name="recursive">When <see langword="true"/> all contents of the directory get deleted.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="true"/> if the directory is deleted; otherwise, <see langword="false"/>.</returns>
    ///  <exception cref="ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
    public static bool TryDeleteWithRetry(this IDirectoryInfo directory, bool recursive = true, int retryCount = 2,
        int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (directory == null)
            throw new ArgumentNullException(nameof(directory));

        return directory.FileSystem.Directory.TryDeleteWithRetry(directory.FullName, recursive, retryCount, retryDelay, errorAction);
    }

    /// <summary>
    /// Tries to delete a specified directory. 
    /// </summary>
    /// <param name="_"></param>
    /// <param name="directory">The directory to delete.</param>
    /// <param name="recursive">When <see langword="true"/> all contents of the directory get deleted.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="false"/> if the operation failed. <see langword="true"/> otherwise.</returns>
    ///  <exception cref="ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
    public static bool TryDeleteWithRetry(this IDirectory _, string directory, bool recursive = true, int retryCount = 2,
        int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (directory == null)
            throw new ArgumentNullException(nameof(directory));

        if (!_.Exists(directory))
            return true;

        return FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay,
            () => _.Delete(directory, recursive), false, errorAction);
    }
}