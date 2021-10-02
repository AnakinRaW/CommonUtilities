using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Sklavenwalker.CommonUtilities.FileSystem
{
    /// <summary>
    /// Service for common task when interacting with the file system.
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Gets the remaining free bytes on the drive where <paramref name="fsItem"/> is located.
        /// </summary>
        /// <param name="fsItem">Some file or directory at the targeted drive.</param>
        /// <returns>free drive space in bytes</returns>
        long GetDriveFreeSpace(IFileSystemInfo fsItem);

        /// <summary>
        /// Tries to create a new file and returns an open stream to the created file. An existing file will be overwritten.
        /// </summary>
        /// <param name="path">The file's location.</param>
        /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
        /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
        /// <returns>Open file stream or <see langword="null"/> if the file could not be created.</returns>
        Stream? CreateFileWithRetry(string path, int retryCount = 2, int retryDelay = 500);

        /// <summary>
        /// Tries to create a new unique folder within the current users temporary directory.
        /// </summary>
        /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
        /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
        /// <returns>The <see cref="IDirectoryInfo"/> of the created folder or <see langword="null"/>.</returns>
        IDirectoryInfo? CreateTemporaryFolderInTempWithRetry(int retryCount = 2, int retryDelay = 500);

        /// <summary>
        /// Tries to copy an existing file. If the <paramref name="destination"/> already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The fil to copy.</param>
        /// <param name="destination">The copy target location.</param>
        /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
        /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
        void CopyFileWithRetry(IFileInfo source, string destination, int retryCount = 2,
            int retryDelay = 500);

        /// <summary>
        /// Moves a file to a new location.
        /// </summary>
        /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
        /// <param name="source">The file or directory.</param>
        /// <param name="destination">The new location.</param>
        /// <param name="replace">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
        /// <returns><see langowrd="true"/>if the operation was successful; <see langowrd="false"/> otherwise.</returns>
        /// <exception cref="IOException">If <paramref name="destination"/> already exists and <paramref name="replace"/>
        /// is <see langword="false"/>.</exception>
        /// <exception cref="FileNotFoundException">if the source was not found.</exception>
        bool MoveFile(IFileInfo source, string destination, bool replace = false);

        /// <summary>
        /// Tries to move a file to a new location.
        /// </summary>
        /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
        /// <param name="source">The file or directory.</param>
        /// <param name="destination">The new location.</param>
        /// <param name="replace">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
        /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
        /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
        /// <returns><see langowrd="true"/>if the operation was successful; <see langowrd="false"/> otherwise.</returns>
        /// <exception cref="IOException">If <paramref name="destination"/> already exists
        /// and <paramref name="replace"/> is <see langword="false"/>.</exception>
        /// <exception cref="FileNotFoundException">if the source was not found.</exception>
        void MoveFileWithRetry(IFileInfo source, string destination, bool replace = false, int retryCount = 2, int retryDelay = 500);

        /// <summary>
        /// Moves a directory to a new location. This also works across drives.
        /// </summary>
        /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
        /// <param name="source">The file or directory.</param>
        /// <param name="destination">The new location.</param>
        /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
        /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
        /// <returns><see langowrd="true"/> if the deletion of the source was successful;
        /// <see langowrd="false"/> if source was not deleted.</returns>
        /// <exception cref="IOException">If <paramref name="destination"/> already exists
        /// and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
        public bool MoveDirectory(IDirectoryInfo source, string destination, IProgress<double>? progress,
            bool overwrite);

        /// <summary>
        /// Tries to moves a directory to a new location. This also works across drives.
        /// </summary>
        /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
        /// <param name="source">The source directory.</param>
        /// <param name="destination">The new location.</param>
        /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
        /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
        /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
        /// <exception cref="IOException">If <paramref name="destination"/> already exists
        /// and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
        void MoveDirectoryWithRetry(IDirectoryInfo source, string destination, bool overwrite = false, int retryCount = 2,
            int retryDelay = 500);

        /// <summary>
        /// Movies a directory asynchronously. This also works across drives.
        /// </summary>
        /// <param name="source">The source directory.</param>
        /// <param name="destination">The new location.</param>
        /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
        /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
        /// <param name="workerCount">Number of parallel workers copying the directory.
        /// If worker count shall be 1 consider using <see cref="MoveDirectory"/>.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><see langowrd="true"/> if the deletion of the source was successful;
        /// <see langowrd="false"/> if source was not deleted.</returns>
        /// <exception cref="IOException">If <paramref name="destination"/> already exists
        /// and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
        Task<bool> MoveDirectoryAsync(IDirectoryInfo source, string destination, IProgress<double>? progress,
            bool overwrite, int workerCount = 2, CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies a directory to a different location.
        /// </summary>
        /// <param name="source">The source directory.</param>
        /// <param name="destination">The new location.</param>
        /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
        /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
        /// <exception cref="DirectoryNotFoundException"> if the source was not found.</exception>
        void CopyDirectory(IDirectoryInfo source, string destination, IProgress<double>? progress, bool overwrite);

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
        /// and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
        void CopyDirectoryWithRetry(IDirectoryInfo source, string destination, bool overwrite = false, int retryCount = 2,
            int retryDelay = 500);

        /// <summary>
        /// Copies a directory asynchronously.
        /// </summary>
        /// <param name="source">The source directory.</param>
        /// <param name="destination">The new location.</param>
        /// <param name="progress">Progress of the operation in percent ranging from 0 to 1. This argument is optional.</param>
        /// <param name="workerCount">Number of parallel workers copying the directory.
        /// If worker count shall be 1 consider using <see cref="CopyDirectory"/>.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The task of the operation.</returns>
        Task CopyDirectoryAsync(IDirectoryInfo source, string destination, IProgress<double>? progress,
            int workerCount = 2, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file if it's somewhere in the user's temporary directory.
        /// </summary>
        /// <param name="file">The file to get deleted.</param>
        void DeleteFileIfInTemp(IFileInfo file);

        /// <summary>
        /// Removes attributes from a given filesystem entry.
        /// </summary>
        /// <param name="fsInfo">The target filesystem handle.</param>
        /// <param name="attributesToRemove">Attributes to remove.</param>
        void RemoveAttributes(IFileSystemInfo fsInfo, FileAttributes attributesToRemove);

        /// <summary>
        /// Set attributes from a given filesystem entry.
        /// </summary>
        /// <param name="fsInfo">The target filesystem handle.</param>
        /// <param name="attributesToAdd">Attributes to add.</param>
        void SetAttributes(IFileInfo fsInfo, FileAttributes attributesToAdd);

        /// <summary>
        /// Tries to delete a file. 
        /// </summary>
        /// <param name="file">The file to delete.</param>
        /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
        /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
        /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
        /// <returns><see langword="false"/> if the operation failed. <see langword="true"/> otherwise.</returns>
        bool DeleteFileWithRetry(IFileInfo file, int retryCount = 2, int retryDelay = 500,
            Func<Exception, int, bool>? errorAction = null);

        /// <summary>
        /// Tries to delete a directory. 
        /// </summary>
        /// <param name="directory">The file to delete.</param>
        /// <param name="recursive">When <see langword="true"/> all contents of the directory get deleted.</param>
        /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
        /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
        /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
        /// <returns><see langword="false"/> if the operation failed.<see langword="false"/> otherwise.</returns>returns>
        bool DeleteDirectoryWithRetry(IDirectoryInfo directory, bool recursive = true, int retryCount = 2,
            int retryDelay = 500, Func<Exception, int, bool>? errorAction = null);
    }
}
