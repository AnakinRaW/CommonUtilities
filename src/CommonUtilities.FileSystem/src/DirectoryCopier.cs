using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.FileSystem;

/// <summary>
/// Service to copy and move directories with progress, file selection callback and cancellation.
/// </summary>
public class DirectoryCopier
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryCopier"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    public DirectoryCopier(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <summary>
    /// Copies a directory to a different location.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0.0 to 1.0. This argument is optional.</param>
    /// <param name="fileFilter">A callback to filter whether a file shall be copied.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="destination"/> is an empty string.</exception>
    /// <exception cref="DirectoryNotFoundException"> <paramref name="source"/> was not found.</exception>
    public void CopyDirectory(
        string source,
        string destination, 
        IProgress<double>? progress = null,
        Predicate<string>? fileFilter = null)
    {
        CopyOrMoveDirectory(source, destination, progress, fileFilter, false);
    }

    /// <summary>
    /// Moves a directory to a new location. This method works also when moving directories across drives.
    /// </summary>
    /// <param name="source">The directory or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0.0 to 1.0. This argument is optional.</param>
    /// <param name="fileFilter">A callback to filter whether a file shall be copied.</param>
    /// <returns><see langowrd="true"/> <paramref name="source"/> was successfully deleted; otherwise, <see langowrd="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="destination"/> is an empty string.</exception>
    /// <exception cref="DirectoryNotFoundException"> <paramref name="source"/> was not found.</exception>
    public bool MoveDirectory(
        string source,
        string destination,
        IProgress<double>? progress = null,
        Predicate<string>? fileFilter = null)
    {
        return CopyOrMoveDirectory(source, destination, progress, fileFilter, true);
    }

    /// <summary>
    /// Copies a directory to a new location. This method works also when moving directories across drives.
    /// </summary>
    /// <param name="source">The directory or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0.0 to 1.0. This argument is optional.</param>
    /// <param name="fileFilter">A callback to filter whether a file shall be copied.</param>
    /// <param name="concurrentWorkers">The number of tasks parallel tasks performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="destination"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><paramref name="concurrentWorkers"/> is zero or negative.</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="source"/> was not found.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    public Task CopyDirectoryAsync(
        string source,
        string destination,
        IProgress<double>? progress = null,
        Predicate<string>? fileFilter = null,
        int concurrentWorkers = 2, 
        CancellationToken cancellationToken = default)
    {
        return CopyOrMoveDirectoryAsync(
            source, destination,
            false,
            progress,
            fileFilter, 
            concurrentWorkers,
            cancellationToken);
    }

    /// <summary>
    /// Moves a directory to a new location. This method works also when moving directories across drives.
    /// </summary>
    /// <remarks>The overwrite functionality may cause data losses of the destination if the operation fails.</remarks>
    /// <param name="source">The directory or directory.</param>
    /// <param name="destination">The new location.</param>
    /// <param name="progress">Progress of the operation in percent ranging from 0.0 to 1.0. This argument is optional.</param>
    /// <param name="fileFilter">A callback to filter whether a file shall be copied.</param>
    /// <param name="concurrentWorkers">The number of tasks parallel tasks performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous move operation and wraps the status information whether <paramref name="source"/> was successfully deleted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="destination"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><paramref name="concurrentWorkers"/> is zero or negative.</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="source"/> was not found.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    public Task<bool> MoveDirectoryAsync(
        string source,
        string destination,
        IProgress<double>? progress = null,
        Predicate<string>? fileFilter = null,
        int concurrentWorkers = 2,
        CancellationToken cancellationToken = default)
    {
        return CopyOrMoveDirectoryAsync(
            source, destination,
            true,
            progress,
            fileFilter,
            concurrentWorkers,
            cancellationToken);
    }

    private async Task<bool> CopyOrMoveDirectoryAsync(
        string source,
        string destination,
        bool isMove,
        IProgress<double>? progress,
        Predicate<string>? fileFilter,
        int concurrentWorkers,
        CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (string.IsNullOrEmpty(source))
            throw new ArgumentException(nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (string.IsNullOrEmpty(destination))
            throw new ArgumentException(nameof(destination));
        
        if (concurrentWorkers <= 0)
            throw new ArgumentException("concurrentWorkers must be greater than zero", nameof(concurrentWorkers));

        cancellationToken.ThrowIfCancellationRequested();

        source = _fileSystem.Path.GetFullPath(source);
        destination = _fileSystem.Path.GetFullPath(destination);

        if (!_fileSystem.Directory.Exists(source))
            throw new DirectoryNotFoundException($"Source directory '{source}' not found");

        progress?.Report(0.0);

        _fileSystem.Directory.CreateDirectory(destination);

        var fileCount = 0;

        using (var queue = new BlockingCollection<CopyInformation>())
        {
            var taskList = new List<Task>(concurrentWorkers)
            {
                Task.Run(() =>
                {
                    AddFilesToQueue(queue, source, destination, isMove, fileFilter, ref fileCount);
                    RunCopyTask(queue, ref fileCount, progress);
                }, cancellationToken)
            };

            for (var index = 1; index < concurrentWorkers; ++index)
            {
                var task = Task.Run(() => RunCopyTask(queue, ref fileCount, progress), cancellationToken);
                taskList.Add(task);
            }

            await Task.WhenAll(taskList);
        }

        progress?.Report(1.0);

        return !isMove || _fileSystem.Directory.TryDeleteWithRetry(source);
    }


    private bool CopyOrMoveDirectory(
        string source,
        string destination,
        IProgress<double>? progress,
        Predicate<string>? fileFilter, bool isMove)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (string.IsNullOrEmpty(source))
            throw new ArgumentException(nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (string.IsNullOrEmpty(destination))
            throw new ArgumentException(nameof(destination));

        source = _fileSystem.Path.GetFullPath(source);
        destination = _fileSystem.Path.GetFullPath(destination);

        if (!_fileSystem.Directory.Exists(source))
            throw new DirectoryNotFoundException($"Source directory '{source}' not found");

        progress?.Report(0.0);

        var filesToCopy = GetFiles(source, destination, isMove, fileFilter);

        var queue = new Queue<CopyInformation>(filesToCopy);
        var totalFileCount = queue.Count;
        while (queue.Count > 0)
        {
            var copyInformation = queue.Dequeue();
            CopyOrMoveFile(copyInformation, queue.Count, ref totalFileCount, progress);
        }

        progress?.Report(1.0);

        return !isMove || _fileSystem.Directory.TryDeleteWithRetry(source);
    }


    private void AddFilesToQueue(BlockingCollection<CopyInformation> queue,
        string source,
        string destination,
        bool isMove,
        Predicate<string>? fileFilter,
        ref int fileCount)
    {
        foreach (var copyInformation in GetFiles(source, destination, isMove, fileFilter))
        {
            ++fileCount;
            queue.Add(copyInformation);
        }

        queue.CompleteAdding();
    }

    private IEnumerable<CopyInformation> GetFiles(string source, string destination, bool isMove, Predicate<string>? fileFilter)
    {
        foreach (var file in _fileSystem.Directory.GetFiles(source, "*", SearchOption.TopDirectoryOnly))
        {
            if (fileFilter is not null && !fileFilter(file))
                continue;

            var localFilePath = file.Substring(source.Length + 1); // +1 for the trailing directory separator
            var destinationPath = _fileSystem.Path.Combine(destination, localFilePath);

            yield return new CopyInformation
            {
                SourceFile = file,
                DestinationFile = destinationPath,
                IsMove = isMove
            };
        }
    }


    private void RunCopyTask(BlockingCollection<CopyInformation> queue, ref int fileCount, IProgress<double>? progress)
    {
        foreach (var copyInfo in queue.GetConsumingEnumerable())
            CopyOrMoveFile(copyInfo, queue.Count, ref fileCount, progress);
    }

    private void CopyOrMoveFile(CopyInformation copyInfo, int remainingFileCount, ref int totalFileCount, IProgress<double>? progress)
    {
        var source = copyInfo.SourceFile;
        var destination = copyInfo.DestinationFile;

        CreateDirectoryOfFile(destination);
        var currentProgress = (totalFileCount - remainingFileCount) / (double)totalFileCount;

        if (copyInfo.IsMove && MoveFile(source, destination))
        {
            progress?.Report(currentProgress);
        }
        else
        {
            if (CopyFile(source, destination) && copyInfo.IsMove)
                DeleteFile(source);
            progress?.Report(currentProgress);
        }
    }

    private bool CopyFile(string sourcePath, string destinationPath)
    {
        try
        {
            _fileSystem.File.CopyWithRetry(sourcePath, destinationPath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void DeleteFile(string sourcePath)
    {
        try
        {
            _fileSystem.File.DeleteWithRetry(sourcePath);
        }
        catch
        {
            // Ignore
        }
    }

    private bool MoveFile(string sourcePath, string destinationPath)
    {
        try
        {
            _fileSystem.File.MoveWithRetry(sourcePath, destinationPath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void CreateDirectoryOfFile(string filePath)
    {
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(filePath)!);
    }

    private readonly struct CopyInformation
    {
        public string SourceFile { get; init; }
        public string DestinationFile { get; init; }
        public bool IsMove { get; init; }
    }
}