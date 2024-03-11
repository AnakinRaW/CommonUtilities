using System;
using System.IO;
using System.IO.Abstractions;

namespace AnakinRaW.CommonUtilities.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="IFileSystemInfo"/> class.
/// </summary>
public static class FileSystemInfoExtensions
{
    /// <summary>
    /// Gets the remaining free bytes on the drive where <paramref name="fsItem"/> is located.
    /// </summary>
    /// <param name="fsItem">Some file or directory at the targeted drive.</param>
    /// <returns>free drive space in bytes</returns>
    public static long GetDriveFreeSpace(this IFileSystemInfo fsItem)
    {
        if (fsItem == null)
            throw new ArgumentNullException(nameof(fsItem));

        var root = fsItem.FileSystem.Path.GetPathRoot(fsItem.FullName);
        return fsItem.FileSystem.DriveInfo.New(root!).AvailableFreeSpace;
    }

    /// <summary>
    /// Removes attributes from a given filesystem entry.
    /// </summary>
    /// <param name="fsInfo">The target filesystem handle.</param>
    /// <param name="attributesToRemove">Attributes to remove.</param>
    public static void RemoveAttributes(this IFileSystemInfo fsInfo, FileAttributes attributesToRemove)
    {
        if (fsInfo == null) 
            throw new ArgumentNullException(nameof(fsInfo));

        var currentAttributes = fsInfo.Attributes;
        var newAttributes = currentAttributes & ~attributesToRemove;
        fsInfo.Attributes = newAttributes;
        fsInfo.Refresh();
    }

    /// <summary>
    /// Set attributes from a given filesystem entry.
    /// </summary>
    /// <param name="fsInfo">The target filesystem handle.</param>
    /// <param name="attributesToAdd">Attributes to add.</param>
    public static void SetAttributes(this IFileSystemInfo fsInfo, FileAttributes attributesToAdd)
    {
        if (fsInfo == null) 
            throw new ArgumentNullException(nameof(fsInfo));

        var currentAttributes = fsInfo.Attributes;
        fsInfo.Attributes = currentAttributes | attributesToAdd;
        fsInfo.Refresh();
    }
}