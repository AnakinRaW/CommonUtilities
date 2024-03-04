using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using AnakinRaW.CommonUtilities.FileSystem.Windows.NativeMethods;
using Microsoft.Win32;
using Validation;

namespace AnakinRaW.CommonUtilities.FileSystem.Windows;

/// <summary>
/// Specialized <see cref="IFileSystemService"/> which is optimized for the use in Windows
/// </summary>
public class WindowsFileExtensions
{
    /// <summary>
    /// Schedules deletion of a file or recursive deletion of a directory after next system restart.
    /// </summary>
    /// <param name="fsInfo"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public bool DeleteAfterReboot(IFileSystemInfo fsInfo)
    {
        Requires.NotNull(fsInfo, nameof(fsInfo));
        if (!fsInfo.Exists)
            return true;
        return fsInfo switch
        {
            IDirectoryInfo directoryInfo => RecursivelyDeleteDirectoryOnReboot(directoryInfo),
            IFileInfo fileInfo => ScheduleDeletionAfterReboot(fileInfo.FullName),
            _ => false
        };
    }

    private bool RecursivelyDeleteDirectoryOnReboot(IDirectoryInfo source)
    {
        var success = true;
        foreach (var directory in source.GetDirectories())
            success &= RecursivelyDeleteDirectoryOnReboot(directory);
        foreach (var file in source.GetFiles())
            success &= ScheduleDeletionAfterReboot(file.FullName);
        return success & ScheduleDeletionAfterReboot(source.FullName);
    }


    private static bool ScheduleDeletionAfterReboot(string source)
    {
        const MoveFileFlags flags = MoveFileFlags.MoveFileDelayUntilReboot | MoveFileFlags.MoveFileWriteThrough;
        var flag = Kernel32.MoveFileEx(source, null, flags);
        return flag || AddPendingFileRename(source, null);
    }

    private static bool AddPendingFileRename(string source, string? destination)
    {
        const string sessionManagerKeyPath = "SYSTEM\\CurrentControlSet\\Control\\Session Manager";
        const string pendingRenameKey = "PendingFileRenameOperations";
        try
        {
            using var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            using var subKey = registryKey.CreateSubKey(sessionManagerKeyPath);
            var stringBuilder = new StringBuilder("\\??\\" + source + "\0");
            if (!string.IsNullOrEmpty(destination))
                stringBuilder.Append("\\??\\" + destination);
            var stringList =
                new List<string>(subKey.GetValue(pendingRenameKey) as string[] ?? Enumerable.Empty<string>())
                {
                    stringBuilder.ToString()
                };
            subKey.SetValue(pendingRenameKey, stringList.ToArray());

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to delete a file. Allows to scheduled deletion on the next system restart.
    /// </summary>
    /// <param name="file">The file to delete.</param>
    /// <param name="rebootRequired"><see langword="true"/> indicates whether the file will be deleted on the system restart.</param>
    /// <param name="rebootOk">When <see langword="true"/> shall be schedule to next system restart if normal deletion fails</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="false"/> if the operation failed.<see langword="false"/> otherwise.</returns>
    /// <exception cref="Exception">If the file could not be deleted and <paramref name="rebootOk"/> was set to <see langword="false"/></exception>
    public bool DeleteFileWithRetry(IFileInfo file, out bool rebootRequired, bool rebootOk = false, int retryCount = 2,
        int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        Requires.NotNull(file, nameof(file));
        if (!file.Exists)
        {
            rebootRequired = false;
            return true;
        }
        var success = ExecuteFileActionWithRetry(retryCount, retryDelay, file.Delete, !rebootOk, (ex, attempt) =>
        {
            if (ex is UnauthorizedAccessException)
            {
                if (attempt == 0)
                {
                    if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        RemoveAttributes(file, FileAttributes.ReadOnly);
                        errorAction?.Invoke(ex, attempt);
                        return true;
                    }
                }
                else if (!rebootOk && attempt == retryCount)
                    throw ex;
            }
            errorAction?.Invoke(ex, attempt);
            return false;
        });
        if (success || !rebootOk)
        {
            rebootRequired = false;
            return success;
        }
        rebootRequired = DeleteAfterReboot(file);
        return false;
    }

    /// <summary>
    /// Tries to delete a file. Allows to scheduled deletion on the next system restart.
    /// </summary>
    /// <param name="directory">The file to delete.</param>
    /// <param name="rebootRequired"><see langword="true"/> indicates whether the file will be deleted on the system restart.</param>
    /// <param name="rebootOk">When <see langword="true"/> shall be schedule to next system restart if normal deletion fails</param>
    /// <param name="recursive">When <see langword="true"/> all contents of the directory get deleted.</param>
    /// <param name="retryCount">Number of retry attempts tempts until the operation fails.</param>
    /// <param name="retryDelay">Delay time in ms between each new attempt.</param>
    /// <param name="errorAction">Callback which gets always triggered if an attempt failed.</param>
    /// <returns><see langword="false"/> if the operation failed.<see langword="false"/> otherwise.</returns>
    /// <exception cref="Exception">If the file could not be deleted and <paramref name="rebootOk"/> was set to <see langword="false"/></exception>
    public bool DeleteDirectoryWithRetry(IDirectoryInfo directory, out bool rebootRequired, bool rebootOk = false,
        bool recursive = true, int retryCount = 2, int retryDelay = 500,
        Func<Exception, int, bool>? errorAction = null)
    {
        Requires.NotNull(directory, nameof(directory));
        if (!directory.Exists)
        {
            rebootRequired = false;
            return true;
        }

        var success = ExecuteFileActionWithRetry(retryCount, retryDelay, () => directory.Delete(recursive), !rebootOk, errorAction);
        if (success || !rebootOk)
        {
            rebootRequired = false;
            return success;
        }

        rebootRequired = DeleteAfterReboot(directory);
        return false;
    }
}