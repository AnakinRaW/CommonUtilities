using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

// From https://github.com/microsoft/vs-threading
public class AwaitExtensionsTests
{
#if !NET5_0_OR_GREATER

    [Fact]
    public async Task WaitForExit_NullArgument()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => AwaitExtensions.WaitForExitAsync(null!));
    }

    [SkippableFact]
    public async Task WaitForExitAsync_ExitCode()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var p = Process.Start(
            new ProcessStartInfo("cmd.exe", "/c exit /b 55")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            })!;
        var exitCode = await p.WaitForExitAsync();
        Assert.Equal(55, exitCode);
    }

    [SkippableFact]
    public void WaitForExitAsync_AlreadyExited()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var p = Process.Start(
            new ProcessStartInfo("cmd.exe", "/c exit /b 55")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            })!;
        p.WaitForExit();
        var t = p.WaitForExitAsync();
        Assert.True(t.IsCompleted);
        Assert.Equal(55, t.Result);
    }

    [Fact]
    public async Task WaitForExitAsync_UnstartedProcess()
    {
        var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
        var process = new Process();
        process.StartInfo.FileName = processName;
        process.StartInfo.CreateNoWindow = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => process.WaitForExitAsync());
    }

    [Fact]
    public async Task WaitForExitAsync_DoesNotCompleteTillKilled()
    {
        var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
        var expectedExitCode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? -1 : 128 + 9; // https://stackoverflow.com/a/1041309
        var p = Process.Start(new ProcessStartInfo(processName) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })!;
        try
        {
            var t = p.WaitForExitAsync();
            Assert.False(t.IsCompleted);
            p.Kill();
            var exitCode = await t;
            Assert.Equal(expectedExitCode, exitCode);
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
    public async Task WaitForExitAsync_Canceled()
    {
        var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
        var p = Process.Start(new ProcessStartInfo(processName) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })!;
        try
        {
            var cts = new CancellationTokenSource();
            var t = p.WaitForExitAsync(cts.Token);
            Assert.False(t.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => t);
        }
        finally
        {
            p.Kill();
        }
    }

#endif
}