using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing.IO;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

// From https://github.com/microsoft/vs-threading
public class AwaitExtensionsTests : ProcessTestBase
{
    [Fact]
    public async Task MultipleProcesses_StartAllKillAllWaitAllAsync()
    {
        const int Iters = 10;
        var processes = Enumerable.Range(0, Iters).Select(_ => CreateProcessLong()).ToArray();

        foreach (var p in processes) p.Start();
        foreach (var p in processes) p.Kill();
        foreach (var p in processes)
        {
            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await p.WaitForExitAsync(cts.Token);
                Assert.True(p.HasExited);
            }
        }
    }

    [Fact]
    public async Task MultipleProcesses_SerialStartKillWaitAsync()
    {
        const int Iters = 10;
        for (int i = 0; i < Iters; i++)
        {
            Process p = CreateProcessLong();
            p.Start();
            p.Kill();
            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await p.WaitForExitAsync(cts.Token);
                Assert.True(p.HasExited);
            }
        }
    }

    [Fact]
    public async Task MultipleProcesses_ParallelStartKillWaitAsync()
    {
        const int Tasks = 4, ItersPerTask = 10;
        Func<Task> work = async () =>
        {
            for (int i = 0; i < ItersPerTask; i++)
            {
                Process p = CreateProcessLong();
                p.Start();
                p.Kill();
                using (var cts = new CancellationTokenSource(WaitInMS))
                {
                    await p.WaitForExitAsync(cts.Token);
                    Assert.True(p.HasExited);
                }
            }
        };

        await Task.WhenAll(Enumerable.Range(0, Tasks).Select(_ => Task.Run(work)));
    }


    //[Fact]
    //public async Task Test_WaitForExitAsync_NullArgument()
    //{
    //    await Assert.ThrowsAsync<ArgumentNullException>(() => AwaitExtensions.WaitForExitAsync(null!));
    //}

    //[PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    //public async Task Test_WaitForExitAsync_ExitCode_Windows()
    //{
    //    var p = Process.Start(
    //        new ProcessStartInfo("cmd.exe", "/c exit /b 55")
    //        {
    //            CreateNoWindow = true,
    //            WindowStyle = ProcessWindowStyle.Hidden,
    //        })!;
    //    await AwaitExtensions.WaitForExitAsync(p);
    //    Assert.Equal(55, p.ExitCode);
    //}

    //[PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    //public void Test_WaitForExitAsync_AlreadyExited_Windows()
    //{
    //    var p = Process.Start(
    //        new ProcessStartInfo("cmd.exe", "/c exit /b 55")
    //        {
    //            CreateNoWindow = true,
    //            WindowStyle = ProcessWindowStyle.Hidden,
    //        })!;
    //    p.WaitForExit();
    //    var t = AwaitExtensions.WaitForExitAsync(p);
    //    Assert.True(t.IsCompleted);
    //    Assert.Equal(55, p.ExitCode);
    //}

    //[Fact]
    //public async Task Test_WaitForExitAsync_UnstartedProcess()
    //{
    //    var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
    //    var process = new Process();
    //    process.StartInfo.FileName = processName;
    //    process.StartInfo.CreateNoWindow = true;
    //    await Assert.ThrowsAsync<InvalidOperationException>(() => process.WaitForExitAsync());
    //}

    //[Fact]
    //public async Task Test_WaitForExitAsync_DoesNotCompleteTillKilled()
    //{
    //    var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
    //    var expectedExitCode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? -1 : 128 + 9; // https://stackoverflow.com/a/1041309
    //    var p = Process.Start(new ProcessStartInfo(processName) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })!;
    //    try
    //    {
    //        var t = AwaitExtensions.WaitForExitAsync(p);
    //        Assert.False(t.IsCompleted);
    //        p.Kill();
    //        await t;
    //        Assert.Equal(expectedExitCode, p.ExitCode);
    //    }
    //    catch
    //    {
    //        try
    //        {
    //            p.Kill();
    //        }
    //        catch
    //        {
    //            // Ignore
    //        }

    //        throw;
    //    }
    //}

    //[Fact]
    //public async Task Test_WaitForExitAsync_Canceled()
    //{
    //    var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
    //    var p = Process.Start(new ProcessStartInfo(processName) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })!;
    //    try
    //    {
    //        var cts = new CancellationTokenSource();
    //        var t = AwaitExtensions.WaitForExitAsync(p, cts.Token);
    //        Assert.False(t.IsCompleted);
    //        cts.Cancel();
    //        await Assert.ThrowsAsync<TaskCanceledException>(() => t);
    //    }
    //    finally
    //    {
    //        p.Kill();
    //    }
    //}
}