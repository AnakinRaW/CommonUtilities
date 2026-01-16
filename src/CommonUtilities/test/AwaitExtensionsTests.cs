using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing.Attributes;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

// From https://github.com/microsoft/vs-threading
public class AwaitExtensionsTests
{
    [Fact]
    public async Task WaitForExitAsync_NullArgument()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => AwaitExtensions.WaitForExitAsync(null!, TestContext.Current.CancellationToken));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task WaitForExitAsync_ExitCode_Windows()
    {
        var p = System.Diagnostics.Process.Start(
            new ProcessStartInfo("cmd.exe", "/c exit /b 55")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            })!;
        await p.WaitForExitAsync();
        Assert.Equal(55, p.ExitCode);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void WaitForExitAsync_AlreadyExited_Windows()
    {
        var p = System.Diagnostics.Process.Start(
            new ProcessStartInfo("cmd.exe", "/c exit /b 55")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            })!;
        p.WaitForExit();
        var t = p.WaitForExitAsync();
        Assert.True(t.IsCompleted);
        Assert.Equal(55, p.ExitCode);
    }

    [Fact]
    public async Task WaitForExitAsync_UnstartedProcess()
    {
        var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = processName;
        process.StartInfo.CreateNoWindow = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => process.WaitForExitAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WaitForExitAsync_DoesNotCompleteTillKilled()
    {
        var p = System.Diagnostics.Process.Start(CreateBlockingProcessStartInfo())!;
        var expectedExitCode =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? -1 : 128 + 9; // https://stackoverflow.com/a/1041309
        try
        {
            var t = p.WaitForExitAsync(TestContext.Current.CancellationToken);
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
    public async Task WaitForExitAsync_Canceled()
    {
        var p = System.Diagnostics.Process.Start(CreateBlockingProcessStartInfo())!;
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

    private static ProcessStartInfo CreateBlockingProcessStartInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c ping -n 300 127.0.0.1 > nul",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
        }

        return new ProcessStartInfo
        {
            FileName = "sleep",
            Arguments = "300",
            CreateNoWindow = true,
            UseShellExecute = false,
        };
    }
}