using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace AnakinRaW.CommonUtilities.FileSystem;

public static class DirectoryExnteions
{
    public static bool MoveToEx(this IDirectoryInfo source, string destination, IProgress<double>? progress,
        DirectoryOverwriteOption overwrite)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        if (!source.Exists)
            throw new DirectoryNotFoundException();
        if (FileSystem.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                FileSystem.Directory.Delete(destination, true);
        }
        var destinationFullPath = FileSystem.Path.GetFullPath(destination);
        if (source.FullName.Equals(destinationFullPath))
            return false;
        progress?.Report(0.0);
        new DirectoryCopier(this, progress).CopyDirectory(source, destination, true);
        var deleteSuccess = DeleteDirectoryWithRetry(source);
        progress?.Report(1.0);
        return deleteSuccess;
    }

    public static void MoveToWithRetry(this IDirectoryInfo source, string destination,
        DirectoryOverwriteOption overwrite = DirectoryOverwriteOption.NoOverwrite,
        int retryCount = 2, int retryDelay = 500)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        ExecuteFileActionWithRetry(retryCount, retryDelay, () => source.MoveToEx(destination, null, overwrite));
    }

    public static async Task<bool> MoveToAsync(this IDirectoryInfo source, string destination, IProgress<double>? progress,
        DirectoryOverwriteOption overwrite, int workerCount = 2, CancellationToken cancellationToken = default)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        cancellationToken.ThrowIfCancellationRequested();
        if (!source.Exists)
            throw new DirectoryNotFoundException($"Directory '{source.FullName}' not found");
        if (FileSystem.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                await Task.Run(() => FileSystem.Directory.Delete(destination, true), cancellationToken);
        }

        var destinationFullPath = FileSystem.Path.GetFullPath(destination);
        if (source.FullName.Equals(destinationFullPath))
            return false;
        progress?.Report(0.0);
        await new DirectoryCopier(this, progress, workerCount).CopyDirectoryAsync(source, destination, true, cancellationToken);
        var deleteSuccess = DeleteDirectoryWithRetry(source);
        progress?.Report(1.0);
        return deleteSuccess;
    }

    public static void Copy(this IDirectoryInfo source, string destination, IProgress<double>? progress, DirectoryOverwriteOption overwrite)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        if (!source.Exists)
            throw new DirectoryNotFoundException($"Directory '{source.FullName}' not found");
        if (FileSystem.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                FileSystem.Directory.Delete(destination, true);
        }
        var destinationFullPath = FileSystem.Path.GetFullPath(destination);
        if (source.FullName.Equals(destinationFullPath))
            return;
        progress?.Report(0.0);
        new DirectoryCopier(this, progress).CopyDirectory(source, destination, false);
        progress?.Report(1.0);
    }

    public static void CopyWithRetry(this IDirectoryInfo source, string destination,
        DirectoryOverwriteOption overwrite = DirectoryOverwriteOption.NoOverwrite,
        int retryCount = 2, int retryDelay = 500)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        ExecuteFileActionWithRetry(retryCount, retryDelay,
            () => CopyDirectory(source, destination, null, overwrite));
    }

    public static async Task CopyAsync(this IDirectoryInfo source, string destination, IProgress<double>? progress,
        DirectoryOverwriteOption overwrite, int workerCount = 2, CancellationToken cancellationToken = default)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        cancellationToken.ThrowIfCancellationRequested();
        if (!source.Exists)
            throw new DirectoryNotFoundException($"Directory '{source.FullName}' not found");
        if (FileSystem.Directory.Exists(destination))
        {
            if (overwrite == DirectoryOverwriteOption.NoOverwrite)
                throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
            if (overwrite == DirectoryOverwriteOption.CleanOverwrite)
                await Task.Run(() => FileSystem.Directory.Delete(destination, true), cancellationToken);
        }
        var destinationFullPath = FileSystem.Path.GetFullPath(destination);
        if (source.FullName.Equals(destinationFullPath))
            return;
        progress?.Report(0.0);
        await new DirectoryCopier(this, progress, workerCount).CopyDirectoryAsync(source, destination, false,
            cancellationToken);
        progress?.Report(1.0);
    }

    public static bool DeleteWithRetry(this IDirectoryInfo directory, bool recursive = true, int retryCount = 2,
        int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        return !directory.Exists || ExecuteFileActionWithRetry(retryCount, retryDelay,
            () => directory.Delete(recursive), errorAction: errorAction);
    }
}