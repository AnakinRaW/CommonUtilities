using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

// From https://github.com/microsoft/vs-threading
public class AwaitExtensionsTests
{
    [Fact]
    public async Task Test_WaitForExitAsync_NullArgument()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => AwaitExtensions.WaitForExitAsync(null!));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task Test_WaitForExitAsync_ExitCode_Windows()
    {
        var p = Process.Start(
            new ProcessStartInfo("cmd.exe", "/c exit /b 55")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            })!;
        await AwaitExtensions.WaitForExitAsync(p);
        Assert.Equal(55, p.ExitCode);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Test_WaitForExitAsync_AlreadyExited_Windows()
    {
        var p = Process.Start(
            new ProcessStartInfo("cmd.exe", "/c exit /b 55")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            })!;
        p.WaitForExit();
        var t = AwaitExtensions.WaitForExitAsync(p);
        Assert.True(t.IsCompleted);
        Assert.Equal(55, p.ExitCode);
    }

    [Fact]
    public async Task Test_WaitForExitAsync_UnstartedProcess()
    {
        var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
        var process = new Process();
        process.StartInfo.FileName = processName;
        process.StartInfo.CreateNoWindow = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => process.WaitForExitAsync());
    }

    [Fact]
    public async Task Test_WaitForExitAsync_DoesNotCompleteTillKilled()
    {
        var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
        var expectedExitCode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? -1 : 128 + 9; // https://stackoverflow.com/a/1041309
        var p = Process.Start(new ProcessStartInfo(processName) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })!;
        try
        {
            var t = AwaitExtensions.WaitForExitAsync(p);
            Assert.False(t.IsCompleted);
            p.Kill();
            await t;
            Assert.Equal(expectedExitCode, p.ExitCode);
        }
        catch
        {
            try
            {
                p.Kill();
            }
            catch
            {
                // Ignore
            }

            throw;
        }
    }

    [Fact]
    public async Task Test_WaitForExitAsync_Canceled()
    {
        var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
        var p = Process.Start(new ProcessStartInfo(processName) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })!;
        try
        {
            var cts = new CancellationTokenSource();
            var t = AwaitExtensions.WaitForExitAsync(p, cts.Token);
            Assert.False(t.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => t);
        }
        finally
        {
            p.Kill();
        }
    }
}