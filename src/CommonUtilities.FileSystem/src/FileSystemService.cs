using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace Sklavenwalker.CommonUtilities.FileSystem
{
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
        public FileSystemService(IFileSystem fileSystem) : this (fileSystem, new PathHelperService(fileSystem))
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
                () => stream = FileSystem.FileStream.Create(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None));
            return stream;
        }

        /// <inheritdoc/>
        public IDirectoryInfo? CreateTemporaryFolderInTempWithRetry(int retryCount = 2, int retryDelay = 500)
        {
            string? createdFolderFullPath = null;
            ExecuteFileActionWithRetry(retryCount, retryDelay, () =>
            {
                createdFolderFullPath = FileSystem.Path.GetRandomFileName();
                FileSystem.Directory.CreateDirectory(createdFolderFullPath);
            });
            return createdFolderFullPath is null ? null : FileSystem.DirectoryInfo.FromDirectoryName(createdFolderFullPath);
        }

        /// <inheritdoc/>
        public virtual void CopyFileWithRetry(IFileInfo source, string destination, int retryCount = 2, int retryDelay = 500)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNullOrEmpty(destination, nameof(destination));
            ExecuteFileActionWithRetry(retryCount, retryDelay, () => source.CopyTo(destination, true));
        }

        /// <inheritdoc/>
        public bool MoveFile(IFileInfo source, string destination, bool replace)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNullOrEmpty(destination, nameof(destination));
#if NET
            source.MoveTo(destination, replace);
#else
            source.CopyTo(destination, replace);
            source.Delete();
#endif
            return true;
        }

        /// <inheritdoc/>
        public void MoveFileWithRetry(IFileInfo source, string destination, bool replace = false, int retryCount = 2,
            int retryDelay = 500)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNullOrEmpty(destination, nameof(destination));
            ExecuteFileActionWithRetry(retryCount, retryDelay, () => MoveFile(source, destination, replace));
        }

        /// <inheritdoc/>
        public bool MoveDirectory(IDirectoryInfo source, string destination, bool overwrite)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNullOrEmpty(destination, nameof(destination));
            if (!source.Exists)
                throw new DirectoryNotFoundException();
            if (FileSystem.Directory.Exists(destination))
            {
                if (!overwrite)
                    throw new IOException($"Cannot create {destination} because directory with the same name already exists.");
                FileSystem.Directory.Delete(destination, true);
            }
            CopyDirectoryRecursive(source, destination);
            source.Delete(true);
            return true;
        }

        private void CopyDirectoryRecursive(IDirectoryInfo source, string destination)
        {
            FileSystem.Directory.CreateDirectory(destination);
            foreach (var file in source.GetFiles())
                file.CopyTo(FileSystem.Path.Combine(destination, file.Name), true);
            foreach (var subDir in source.GetDirectories())
            {
                var newSubDirPath = FileSystem.Path.Combine(destination, subDir.Name);
                CopyDirectoryRecursive(subDir, newSubDirPath);
            }
        }

        /// <inheritdoc/>
        public void MoveDirectoryWithRetry(IDirectoryInfo source, string destination, bool replace = false, int retryCount = 2,
            int retryDelay = 500)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNullOrEmpty(destination, nameof(destination));
            ExecuteFileActionWithRetry(retryCount, retryDelay, () => MoveDirectory(source, destination, replace));
        }


        public async Task<bool> MoveDirectoryAsync(IDirectoryInfo source, string destination, IProgress<double>? progress,
            CancellationToken cancellationToken = default)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNullOrEmpty(destination, nameof(destination));
            cancellationToken.ThrowIfCancellationRequested();
            if (!source.Exists)
                throw new DirectoryNotFoundException($"Directory '{source.FullName}' not found");
            progress?.Report(0.0);
            await Task.Delay(0);
            var deleteSuccess = DeleteDirectoryWithRetry(source);
            progress?.Report(1.0);
            return deleteSuccess;
        }


        /// <summary>
        /// Copies a directory asynchronously.
        /// </summary>
        /// <param name="source">The source directory.</param>
        /// <param name="destination">The target location.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task which movies the directory.</returns>
        public async Task CopyDirectoryAsync(IDirectoryInfo source, string destination, IProgress<double>? progress,
            CancellationToken cancellationToken = default)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNullOrEmpty(destination, nameof(destination));
            cancellationToken.ThrowIfCancellationRequested();
            if (!source.Exists)
                throw new DirectoryNotFoundException($"Directory '{source.FullName}' not found");
            progress?.Report(0.0);
            await Task.Delay(0);
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
            private readonly IFileSystem _fileSystem;
            private int MaximumConcurrency { get; }

            public DirectoryCopier(IFileSystem fileSystem, int maximumConcurrency)
            {
                _fileSystem = fileSystem;
                MaximumConcurrency = maximumConcurrency;
            }

            public async Task CopyDirectoryAsync(IDirectoryInfo source, string destination, bool isMove, CancellationToken cancellationToken)
            {
                _fileSystem.Directory.CreateDirectory(destination);
                var fileCount = 0;
                using BlockingCollection<CopyInformation> queue = new();
                List<Task> taskList = new(MaximumConcurrency)
                {
                    Task.Run(
                        () => EnumerateFiles(source, destination, queue, isMove, ref fileCount), cancellationToken)
                };
                for (var index = 1; index < MaximumConcurrency; ++index)
                {
                    Task task = Task.Run(() => RunCopyTask(source, destination, queue, ref fileCount), cancellationToken);
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

                    queue.Add(copyInformation);
                    ++fileCount;
                }

                queue.CompleteAdding();
                RunCopyTask(source, destination, queue, ref fileCount);
            }


            private void RunCopyTask(IDirectoryInfo source, string destination, BlockingCollection<CopyInformation> queue, ref int fileCount)
            {
                foreach (var copyInfo in queue.GetConsumingEnumerable())
                {
                    var fileToCopy = copyInfo.File;
                    string path2 = fileToCopy.FullName.Substring(source.FullName.Length + 1);
                    string newFilePath = _fileSystem.Path.Combine(destination, path2);
                    CreateDirectoryOfFile(newFilePath);
                    var progress = (fileCount - queue.Count) / fileCount;
                    if (copyInfo.IsMove && InvokeMoveOperation(fileToCopy, newFilePath))
                    {
                        OnProgress(progress);
                    }
                    else
                    {
                        if (InvokeCopyOperation(fileToCopy, newFilePath) && copyInfo.IsMove)
                            InvokeDeleteOperation(fileToCopy);
                        OnProgress(progress);
                    }
                }
            }

            private void CreateDirectoryOfFile(string filePath)
            {
                var directoryPath = _fileSystem.Path.GetDirectoryName(filePath);
                _fileSystem.Directory.CreateDirectory(directoryPath);
            }

            private struct CopyInformation
            {
                public IFileInfo File;
                public bool IsMove;
            }
        }
    }
}