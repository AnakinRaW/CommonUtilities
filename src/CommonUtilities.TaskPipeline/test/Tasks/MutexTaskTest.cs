using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Sklavenwalker.CommonUtilities.TaskPipeline.Tasks;
using Xunit;

namespace Sklavenwalker.CommonUtilities.TaskPipeline.Test.Tasks;

public class MutexTaskTest
{
    [Fact]
    public void TestDispose()
    {
        var sc = new ServiceCollection();
        var task = new AcquireMutexTask("MyMutex", sc.BuildServiceProvider());
        Assert.Equal("MyMutex", task.MutexName);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            task.Run(default);
        }
        else
        {
            Assert.Throws<PlatformNotSupportedException>(() => task.Run(default));
            return;
        }
        task.Dispose();
    }
}