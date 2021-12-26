using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace Sklavenwalker.CommonUtilities.FileSystem;

/// <inheritdoc cref="IFileSystemService"/>
public class FileSystemService : IFileSystemService
{
    /// <summary>
    /// Holds the <see cref="IFileSystem"/> instance.
    /// </summary>
    protected readonly IFileSystem FileSystem;

    /// <summary>
    /// Holds an instance of <see cref="IPathHelperService"/>.
    /// </summary>
    protected readonly IPathHelperService PathHelperService;

    /// <summary>
    /// Creates a new instance with a given <see cref="IFileSystem"/>.
    /// </summary>
    /// <param name="fileSystem"></param>
    public FileSystemService(IFileSystem fileSystem) : this(fileSystem, new PathHelperService(fileSystem))
    {
        Requires.NotNull(fileSystem, nameof(fileSystem));
        FileSystem = fileSystem;
    }

    /// <summary>
    /// Creates a new instance with the default <see cref="IFileSystem"/> implementation for this system.
    /// </summary>
    public FileSystemService() : this(new System.IO.Abstractions.FileSystem(), new PathHelperService())
    {
    }

    internal FileSystemService(IFileSystem fileSystem, IPathHelperService pathHelperService)
    {
        FileSystem = fileSystem;
        PathHelperService = pathHelperService;
    }

    /// <inheritdoc/>
    public virtual long GetDriveFreeSpace(IFileSystemInfo fsItem)
    {
        Requires.NotNull(fsItem, nameof(fsItem));
        var pathInstance = FileSystem.Path;
        var root = pathInstance.GetPathRoot(fsItem.FullName);
        return FileSystem.DriveInfo.FromDriveName(root).AvailableFreeSpace;
    }

    /// <inheritdoc/>
    public virtual Stream? CreateFileWithRetry(string path, int retryCount = 2, int retryDelay = 500)
    {
        Requires.NotNullOrEmpty(path, nameof(path));
        Stream? stream = null;
        ExecuteFileActionWithRetry(retryCount, retryDelay,
            () => stream =
                FileSystem.FileStream.Create(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None));
        return stream;
    }

    /// <inheritdoc/>
    public IDirectoryInfo? CreateTemporaryFolderInTempWithRetry(int retryCount = 2, int retryDelay = 500)
    {
        IDirectoryInfo? newTempFolder = null;
        ExecuteFileActionWithRetry(retryCount, retryDelay, () =>
        {
            var tempFolder = FileSystem.Path.GetTempPath();
            var folderName = FileSystem.Path.GetRandomFileName();
            var fullFolderPath = FileSystem.Path.Combine(tempFolder, folderName);
            newTempFolder = FileSystem.Directory.CreateDirectory(fullFolderPath);
        });
        return newTempFolder;
    }

    /// <inheritdoc/>
    public virtual void CopyFileWithRetry(IFileInfo source, string destination, int retryCount = 2,
        int retryDelay = 500)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        ExecuteFileActionWithRetry(retryCount, retryDelay, () => source.CopyTo(destination, true));
    }

    /// <inheritdoc/>
    public bool MoveFile(IFileInfo source, string destination, bool overwrite)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
#if NET
        source.MoveTo(destination, overwrite);
#else
            source.CopyTo(destination, overwrite);
            source.Delete();
#endif
        return true;
    }

    /// <inheritdoc/>
    public void MoveFileWithRetry(IFileInfo source, string destination, bool overwrite = false, int retryCount = 2,
        int retryDelay = 500)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        ExecuteFileActionWithRetry(retryCount, retryDelay, () => MoveFile(source, destination, overwrite));
    }
        
    /// <inheritdoc/>
    public bool MoveDirectory(IDirectoryInfo source, string destination, IProgress<double>? progress,
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

    /// <inheritdoc/>
    public void MoveDirectoryWithRetry(IDirectoryInfo source, string destination, 
        DirectoryOverwriteOption overwrite = DirectoryOverwriteOption.NoOverwrite,
        int retryCount = 2, int retryDelay = 500)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        ExecuteFileActionWithRetry(retryCount, retryDelay, () => MoveDirectory(source, destination, null, overwrite));
    }

    /// <inheritdoc/>
    public async Task<bool> MoveDirectoryAsync(IDirectoryInfo source, string destination, IProgress<double>? progress,
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

    /// <inheritdoc/>
    public void CopyDirectory(IDirectoryInfo source, string destination, IProgress<double>? progress, DirectoryOverwriteOption overwrite)
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

    /// <inheritdoc/>
    public void CopyDirectoryWithRetry(IDirectoryInfo source, string destination,
        DirectoryOverwriteOption overwrite = DirectoryOverwriteOption.NoOverwrite, 
        int retryCount = 2, int retryDelay = 500)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        ExecuteFileActionWithRetry(retryCount, retryDelay,
            () => CopyDirectory(source, destination, null, overwrite));
    }

    /// <inheritdoc/>
    public async Task CopyDirectoryAsync(IDirectoryInfo source, string destination, IProgress<double>? progress,
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



    /// <inheritdoc/>
    public void DeleteFileIfInTemp(IFileInfo file)
    {
        Requires.NotNull(file, nameof(file));
        if (!file.Exists || !PathHelperService.IsChildOf(FileSystem.Path.GetTempPath(), file.FullName))
            return;
        file.Delete();
    }

    /// <inheritdoc/>
    public void RemoveAttributes(IFileSystemInfo fsInfo, FileAttributes attributesToRemove)
    {
        Requires.NotNull(fsInfo, nameof(fsInfo));
        var currentAttributes = fsInfo.Attributes;
        var newAttributes = currentAttributes & ~attributesToRemove;
        fsInfo.Attributes = newAttributes;
        fsInfo.Refresh();
    }

    /// <inheritdoc/>
    public void SetAttributes(IFileInfo fsInfo, FileAttributes attributesToAdd)
    {
        Requires.NotNull(fsInfo, nameof(fsInfo));
        var currentAttributes = fsInfo.Attributes;
        fsInfo.Attributes = currentAttributes | attributesToAdd;
        fsInfo.Refresh();
    }

    /// <inheritdoc/>
    public bool DeleteFileWithRetry(IFileInfo file, int retryCount = 2, int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (!file.Exists)
            return true;
        return ExecuteFileActionWithRetry(retryCount, retryDelay, file.Delete, errorAction: (ex, attempt) =>
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
            }
            errorAction?.Invoke(ex, attempt);
            return false;
        });

    }

    /// <inheritdoc/>
    public bool DeleteDirectoryWithRetry(IDirectoryInfo directory, bool recursive = true, int retryCount = 2,
        int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        return !directory.Exists || ExecuteFileActionWithRetry(retryCount, retryDelay,
            () => directory.Delete(recursive), errorAction: errorAction);
    }

    /// <summary>
    /// Tries to execute a given IO action. In the event of an <see cref="IOException"/>
    /// or <see cref="UnauthorizedAccessException"/> the action will be tried again.
    /// </summary>
    /// <param name="retryCount">Amount of retries of <paramref name="fileAction"/></param>
    /// <param name="retryDelay">The delay in ms between each retry.</param>
    /// <param name="fileAction">The action the get performed.</param>
    /// <param name="throwOnFailure">When set to <see langword="true"/>, if all retries are unsuccessful the causing exception will be thrown.</param>
    /// <param name="errorAction">Callback which gets invoked if an <see cref="IOException"/>
    /// or <see cref="UnauthorizedAccessException"/> is was thrown during the <paramref name="fileAction"/> execution..</param>
    /// <returns><see langword="true"/>if the operation was successful. <see langword="false"/> otherwise.</returns>
    protected bool ExecuteFileActionWithRetry(int retryCount, int retryDelay, Action fileAction,
        bool throwOnFailure = true, Func<Exception, int, bool>? errorAction = null)
    {
        var num = retryCount + 1;
        for (var index = 0; index < num; ++index)
        {
            try
            {
                fileAction();
                return true;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                if (!throwOnFailure || index + 1 < num)
                {
                    if (errorAction != null)
                    {
                        if (!errorAction(ex, index))
                        {
                            if (index + 1 >= num)
                                continue;
                        }
                        else
                            continue;
                    }

                    Task.Delay(retryDelay).Wait();
                }
                else
                    throw;
            }
        }
        return false;
    }

    private class DirectoryCopier
    {
        private readonly FileSystemService _service;
        private readonly IProgress<double>? _progress;
        private int MaximumConcurrency { get; }

        public DirectoryCopier(FileSystemService service, IProgress<double>? progress, int maximumConcurrency = 1)
        {
            _service = service;
            _progress = progress;
            MaximumConcurrency = maximumConcurrency;
            if (MaximumConcurrency <= 0)
                throw new InvalidOperationException("MaximumConcurrency must be greater than zero");
        }

        public void CopyDirectory(IDirectoryInfo source, string destination, bool isMove)
        {
            var filesToCopy = source.GetFiles("*", SearchOption.AllDirectories)
                .Select(f => new CopyInformation { File = f, IsMove = isMove })
                .ToList();
            var totalFileCount = filesToCopy.Count;
            var queue = new Queue<CopyInformation>(filesToCopy);
            while (queue.Count > 0)
            {
                var copyInformation = queue.Dequeue();
                CopyFile(source, copyInformation, destination, queue.Count, ref totalFileCount);
            }
        }

        public async Task CopyDirectoryAsync(IDirectoryInfo source, string destination, bool isMove, CancellationToken cancellationToken)
        {
            _service.FileSystem.Directory.CreateDirectory(destination);
            var fileCount = 0;
            using BlockingCollection<CopyInformation> queue = new();
            List<Task> taskList = new(MaximumConcurrency)
            {
                Task.Run(() => EnumerateFiles(source, destination, queue, isMove, ref fileCount), cancellationToken)
            };
            for (var index = 1; index < MaximumConcurrency; ++index)
            {
                var task = Task.Run(() => RunCopyTask(source, destination, queue, ref fileCount), cancellationToken);
                taskList.Add(task);
            }
            await Task.WhenAll(taskList);
        }

        private void EnumerateFiles(IDirectoryInfo source, string destination,
            BlockingCollection<CopyInformation> queue, bool isMove, ref int fileCount)
        {
            foreach (var file in source.GetFiles("*", SearchOption.AllDirectories))
            {
                var copyInformation = new CopyInformation
                {
                    File = file,
                    IsMove = isMove
                };
                ++fileCount;
                queue.Add(copyInformation);
            }

            queue.CompleteAdding();
            RunCopyTask(source, destination, queue, ref fileCount);
        }


        private void RunCopyTask(IDirectoryInfo source, string destination,
            BlockingCollection<CopyInformation> queue, ref int fileCount)
        {
            foreach (var copyInfo in queue.GetConsumingEnumerable())
                CopyFile(source, copyInfo, destination, queue.Count, ref fileCount);
        }

        private void CopyFile(IDirectoryInfo source, CopyInformation copyInfo, string destination,
            int remainingFileCount, ref int totalFileCount)
        {
            var fileToCopy = copyInfo.File;
            var localFilePath = fileToCopy.FullName.Substring(source.FullName.Length + 1);
            var newFilePath = _service.FileSystem.Path.Combine(destination, localFilePath);
            CreateDirectoryOfFile(newFilePath);
            var currentProgress = (totalFileCount - remainingFileCount) / totalFileCount;
            if (copyInfo.IsMove && InvokeMoveOperation(fileToCopy, newFilePath))
            {
                _progress?.Report(currentProgress);
            }
            else
            {
                if (InvokeCopyOperation(fileToCopy, newFilePath) && copyInfo.IsMove)
                    InvokeDeleteOperation(fileToCopy);
                _progress?.Report(currentProgress);
            }
        }

        private bool InvokeCopyOperation(IFileInfo sourcePath, string destinationPath)
        {
            try
            {
                _service.CopyFileWithRetry(sourcePath, destinationPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void InvokeDeleteOperation(IFileInfo sourcePath)
        {
            try
            {
                _service.DeleteFileWithRetry(sourcePath);
            }
            catch
            {
                // Ignore
            }
        }

        private bool InvokeMoveOperation(IFileInfo sourcePath, string destinationPath)
        {
            try
            {
                _service.MoveFileWithRetry(sourcePath, destinationPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void CreateDirectoryOfFile(string filePath)
        {
            var directoryPath = _service.FileSystem.Path.GetDirectoryName(filePath);
            _service.FileSystem.Directory.CreateDirectory(directoryPath);
        }

        private struct CopyInformation
        {
            public IFileInfo File;
            public bool IsMove;
        }
    }
}