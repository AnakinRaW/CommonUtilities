using System;
using System.IO;
using System.IO.Abstractions;

namespace AnakinRaW.CommonUtilities.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="IFileSystemInfo"/> class.
/// </summary>
public static class FileSystemInfoExtensions
{
    /// <param name="fsItem">Some file or directory at the targeted drive.</param>
    extension(IFileSystemInfo fsItem)
    {
        /// <summary>
        /// Gets the remaining free bytes on the drive where <paramref name="fsItem"/> is located.
        /// </summary>
        /// <returns>free drive space in bytes</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fsItem"/> is <see langword="null"/>.</exception>
        public long GetDriveFreeSpace()
        {
            if (fsItem == null)
                throw new ArgumentNullException(nameof(fsItem));

            var root = fsItem.FileSystem.Path.GetPathRoot(fsItem.FullName);
            return fsItem.FileSystem.DriveInfo.New(root!).AvailableFreeSpace;
        }

        /// <summary>
        /// Removes attributes from a given filesystem entry.
        /// </summary>
        /// <param name="attributesToRemove">Attributes to remove.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fsItem"/> is <see langword="null"/>.</exception>
        public void RemoveAttributes(FileAttributes attributesToRemove)
        {
            if (fsItem == null) 
                throw new ArgumentNullException(nameof(fsItem));

            var currentAttributes = fsItem.Attributes;
            var newAttributes = currentAttributes & ~attributesToRemove;
            fsItem.Attributes = newAttributes;
            fsItem.Refresh();
        }

        /// <summary>
        /// Set attributes from a given filesystem entry.
        /// </summary>
        /// <param name="attributesToAdd">Attributes to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fsItem"/> is <see langword="null"/>.</exception>
        public void SetAttributes(FileAttributes attributesToAdd)
        {
            if (fsItem == null) 
                throw new ArgumentNullException(nameof(fsItem));

            var currentAttributes = fsItem.Attributes;
            fsItem.Attributes = currentAttributes | attributesToAdd;
            fsItem.Refresh();
        }
    }
}