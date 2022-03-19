using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Sklavenwalker.CommonUtilities.TaskPipeline.Test;

public class MutexTest
{
    [Fact(Skip = "Randomly fails in CI.")]
    public void Test()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Throws<PlatformNotSupportedException>(() => Utilities.EnsureMutex("MyMutex"));
            return;
        }

        var mutex = Utilities.EnsureMutex("MyMutex");
        Assert.NotNull(mutex);

        var other = Utilities.EnsureMutex("MyMutex");
        Assert.NotNull(other);

        mutex.ReleaseMutex();
        other.ReleaseMutex();
    }
}