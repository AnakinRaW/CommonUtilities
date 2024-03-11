using System;
using System.IO;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class FileSystemUtilitiesTest
{
    [Fact]
    public void Test_ExecuteFileActionWithRetry_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FileSystemUtilities.ExecuteFileSystemActionWithRetry(-1, 0, () => { }));
        Assert.Throws<ArgumentNullException>(() => FileSystemUtilities.ExecuteFileSystemActionWithRetry(0, 0, null!));
        Assert.Throws<IOException>(() => FileSystemUtilities.ExecuteFileSystemActionWithRetry(0, 0, () => throw new IOException()));
        Assert.Throws<UnauthorizedAccessException>(() => FileSystemUtilities.ExecuteFileSystemActionWithRetry(0, 0, () => throw new UnauthorizedAccessException()));
        Assert.Throws<Exception>(() => FileSystemUtilities.ExecuteFileSystemActionWithRetry(0, 0, () => throw new Exception()));
        Assert.Throws<Exception>(() => FileSystemUtilities.ExecuteFileSystemActionWithRetry(0, 0, () => throw new Exception(), false));
    }

    [Fact]
    public void Test_ExecuteFileActionWithRetry_ErrorAction()
    {
        var errorAction = 0;
        Assert.False(FileSystemUtilities.ExecuteFileSystemActionWithRetry(2, 0, () => throw new IOException(), false,
            (_, _) =>
            {
                errorAction++;
                return false;
            }));

        Assert.Equal(3, errorAction);

        Assert.False(FileSystemUtilities.ExecuteFileSystemActionWithRetry(2, 0, () => throw new IOException(), false,
            (_, _) =>
            {
                errorAction++;
                return true;
            }));

        Assert.Equal(6, errorAction);
    }

    [Fact]
    public void Test_ExecuteFileActionWithRetry()
    {
        var actionRunCount = 0;
        Assert.True(FileSystemUtilities.ExecuteFileSystemActionWithRetry(2, 0, () =>
        {
            actionRunCount++;
        }));
        Assert.Equal(1, actionRunCount);
    }

    [Fact]
    public void Test_ExecuteFileActionWithRetry_Retry()
    {
        var actionRunCount = 0;
        var fail = true;
        Assert.True(FileSystemUtilities.ExecuteFileSystemActionWithRetry(2, 0, () =>
        {
            actionRunCount++;
            if (fail)
            {
                fail = false;
                throw new IOException();
            }
        }));
        Assert.Equal(2, actionRunCount);
    }
}