using System;
using System.IO;
using System.IO.Abstractions;

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
        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () => source.CopyTo(destination, true));
    }


#if !NET
    /// <summary>
    /// Moves a file to a new location.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
    /// <param name="source">The file or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <returns><see langowrd="true"/>if the operation was successful; <see langowrd="false"/> otherwise.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists and <paramref name="overwrite"/>
    /// is <see langword="false"/>.</exception>
    /// <exception cref="FileNotFoundException">if the source was not found.</exception>
    public static bool MoveTo(this IFileInfo source, string destination, bool overwrite)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        source.CopyTo(destination, overwrite);
        source.Delete();
        return true;
    }
#endif

    /// <summary>
    /// Tries to move a file to a new location.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails. </remarks>
    /// <param name="source">The file or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="overwrite">Indicates whether the <paramref name="destination"/> shall be replaced if it already exists.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <returns><see langowrd="true"/>if the operation was successful; <see langowrd="false"/> otherwise.</returns>
    /// <exception cref="IOException">If <paramref name="destination"/> already exists
    /// and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
    /// <exception cref="FileNotFoundException">if the source was not found.</exception>
    public static void MoveWithRetry(this IFileInfo source, string destination, bool overwrite = false, int retryCount = 2, int retryDelay = 500)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () => source.MoveTo(destination, overwrite));
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
    /// Tries to delete a file. 
    /// </summary>
    /// <param name="file">The file to delete.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="false"/> if the operation failed. <see langword="true"/> otherwise.</returns>
    public static bool DeleteWithRetry(this IFileInfo file, int retryCount = 2, int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (!file.Exists)
            return true;
        return FileSystemUtilities.ExecuteFileSystemActionWithRetry(retryCount, retryDelay, file.Delete, errorAction: (ex, attempt) =>
        {
            if (ex is UnauthorizedAccessException)
            {
                if (attempt == 0)
                {
                    if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        file.RemoveAttributes(FileAttributes.ReadOnly);
                        errorAction?.Invoke(ex, attempt);
                        return true;
                    }
                }
            }
            errorAction?.Invoke(ex, attempt);
            return false;
        });

    }
}