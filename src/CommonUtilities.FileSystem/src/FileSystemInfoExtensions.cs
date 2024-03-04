using System.IO;
using System.IO.Abstractions;
using Validation;

namespace AnakinRaW.CommonUtilities.FileSystem;

public static class FileSystemInfoExtensions
{
    public static long GetDriveFreeSpace(this IFileSystemInfo fsItem)
    {
        Requires.NotNull(fsItem, nameof(fsItem));
        var root = fsItem.FileSystem.Path.GetPathRoot(fsItem.FullName);
        return fsItem.FileSystem.DriveInfo.New(root!).AvailableFreeSpace;
    }

    public static void RemoveAttributes(this IFileSystemInfo fsInfo, FileAttributes attributesToRemove)
    {
        Requires.NotNull(fsInfo, nameof(fsInfo));
        var currentAttributes = fsInfo.Attributes;
        var newAttributes = currentAttributes & ~attributesToRemove;
        fsInfo.Attributes = newAttributes;
        fsInfo.Refresh();
    }

    public static void SetAttributes(this IFileSystemInfo fsInfo, FileAttributes attributesToAdd)
    {
        Requires.NotNull(fsInfo, nameof(fsInfo));
        var currentAttributes = fsInfo.Attributes;
        fsInfo.Attributes = currentAttributes | attributesToAdd;
        fsInfo.Refresh();
    }
}