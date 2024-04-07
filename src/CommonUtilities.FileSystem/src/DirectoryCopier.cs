using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.FileSystem;

internal class DirectoryCopier
{
    private readonly IFileSystem _fileSystem;
    private readonly IProgress<double>? _progress;
    private int MaximumConcurrency { get; }

    public DirectoryCopier(IFileSystem fileSystem, IProgress<double>? progress, int maximumConcurrency = 1)
    {
        _fileSystem = fileSystem;
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
        _fileSystem.Directory.CreateDirectory(destination);
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


    private void RunCopyTask(IDirectoryInfo source, string destination, BlockingCollection<CopyInformation> queue, ref int fileCount)
    {
        foreach (var copyInfo in queue.GetConsumingEnumerable())
            CopyFile(source, copyInfo, destination, queue.Count, ref fileCount);
    }

    private void CopyFile(IDirectoryInfo source, CopyInformation copyInfo, string destination,
        int remainingFileCount, ref int totalFileCount)
    {
        var fileToCopy = copyInfo.File;
        var localFilePath = fileToCopy.FullName.Substring(source.FullName.Length + 1);
        var newFilePath = _fileSystem.Path.Combine(destination, localFilePath);
        CreateDirectoryOfFile(newFilePath);
        var currentProgress = (totalFileCount - remainingFileCount) / (double)totalFileCount;
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
            sourcePath.CopyWithRetry(destinationPath);
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
            sourcePath.DeleteWithRetry();
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
            sourcePath.MoveWithRetry(destinationPath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void CreateDirectoryOfFile(string filePath)
    {
        var directoryPath = _fileSystem.Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directoryPath))
            return;
        _fileSystem.Directory.CreateDirectory(directoryPath!);
    }

    private struct CopyInformation
    {
        public IFileInfo File;
        public bool IsMove;
    }
}