using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing.IO;
using Microsoft.DotNet.RemoteExecutor;
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

    [Theory]
    [InlineData(0)]  // poll
    [InlineData(10)] // real timeout
    public async Task CurrentProcess_WaitAsyncNeverCompletes(int milliseconds)
    {
        using (var cts = new CancellationTokenSource(milliseconds))
        {
            CancellationToken token = cts.Token;
            Process process = Process.GetCurrentProcess();
            OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => process.WaitForExitAsync(token));
            Assert.Equal(token, ex.CancellationToken);
            Assert.False(process.HasExited);
        }
    }

    [Fact]
    public async Task SingleProcess_TryWaitAsyncMultipleTimesBeforeCompleting()
    {
        Process p = CreateProcessLong();
        p.Start();

        // Verify we can try to wait for the process to exit multiple times

        // First test with an already canceled token. Because the token is already canceled,
        // WaitForExitAsync should complete synchronously
        for (int i = 0; i < 2; i++)
        {
            var token = new CancellationToken(canceled: true);
            Task t = p.WaitForExitAsync(token);

            Assert.Equal(TaskStatus.Canceled, t.Status);

            OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            Assert.Equal(token, ex.CancellationToken);
            Assert.False(p.HasExited);
        }

        // Next, test with a token that is canceled after the task is created to
        // exercise event hookup and async cancellation
        using (var cts = new CancellationTokenSource())
        {
            CancellationToken token = cts.Token;
            Task t = p.WaitForExitAsync(token);
            cts.Cancel();

            OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            Assert.Equal(token, ex.CancellationToken);
            Assert.False(p.HasExited);
        }

        // Then wait until it exits and concurrently kill it.
        // There's a race condition here, in that we really want to test
        // killing it while we're waiting, but we could end up killing it
        // before hand, in which case we're simply not testing exactly
        // what we wanted to test, but everything should still work.
        _ = Task.Delay(10).ContinueWith(_ => p.Kill());

        using (var cts = new CancellationTokenSource(WaitInMS))
        {
            await p.WaitForExitAsync(cts.Token);
            Assert.True(p.HasExited);
        }

        // Waiting on an already exited process should complete synchronously
        Assert.True(p.HasExited);
        Task task = p.WaitForExitAsync();
        Assert.Equal(TaskStatus.RanToCompletion, task.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SingleProcess_WaitAsyncAfterExited(bool addHandlerBeforeStart)
    {
        Process p = CreateProcessLong();
        p.EnableRaisingEvents = true;

        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (addHandlerBeforeStart)
        {
            p.Exited += delegate { tcs.SetResult(null); };
        }
        p.Start();
        if (!addHandlerBeforeStart)
        {
            p.Exited += delegate { tcs.SetResult(null); };
        }

        p.Kill();
        await tcs.Task;

        var token = new CancellationToken(canceled: true);
        await p.WaitForExitAsync(token);
        Assert.True(p.HasExited);

        await p.WaitForExitAsync();
        Assert.True(p.HasExited);
    }

    [Fact]
    public async Task SingleProcess_CopiesShareExitAsyncInformation()
    {
        using Process p = CreateProcessLong();
        p.Start();

        Process[] copies = Enumerable.Range(0, 3).Select(_ => Process.GetProcessById(p.Id)).ToArray();

        using (var cts = new CancellationTokenSource(millisecondsDelay: 0))
        {
            CancellationToken token = cts.Token;
            OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => p.WaitForExitAsync(token));
            Assert.Equal(token, ex.CancellationToken);
            Assert.False(p.HasExited);
        }
        p.Kill();
        using (var cts = new CancellationTokenSource(WaitInMS))
        {
            await p.WaitForExitAsync(cts.Token);
            Assert.True(p.HasExited);
        }

        using (var cts = new CancellationTokenSource(millisecondsDelay: 0))
        {
            foreach (Process copy in copies)
            {
                // Since the process has already exited, waiting again does not throw (even if the token is canceled) because
                // there's no work to do.
                await copy.WaitForExitAsync(cts.Token);
                Assert.True(copy.HasExited);
            }
        }
    }

    [Fact]
    public async Task WaitAsyncForPeerProcess()
    {
        using Process child1 = CreateProcessLong();
        child1.Start();

        using Process child2 = CreateProcess(async peerId =>
        {
            Process peer = Process.GetProcessById(int.Parse(peerId));
            Console.WriteLine("Signal");
            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await peer.WaitForExitAsync(cts.Token);
                Assert.True(peer.HasExited);
            }
            return RemoteExecutor.SuccessExitCode;
        }, child1.Id.ToString());
        child2.StartInfo.RedirectStandardOutput = true;
        child2.Start();
        char[] output = new char[6];
        child2.StandardOutput.Read(output, 0, output.Length);
        Assert.Equal("Signal", new string(output)); // wait for the signal before killing the peer

        child1.Kill();
        using (var cts = new CancellationTokenSource(WaitInMS))
        {
            await child1.WaitForExitAsync(cts.Token);
            Assert.True(child1.HasExited);
        }
        using (var cts = new CancellationTokenSource(WaitInMS))
        {
            await child2.WaitForExitAsync(cts.Token);
            Assert.True(child2.HasExited);
        }

        Assert.Equal(RemoteExecutor.SuccessExitCode, child2.ExitCode);
    }

    [Fact]
    public async Task WaitAsyncForSignal()
    {
        const string expectedSignal = "Signal";
        const string successResponse = "Success";
        const int timeout = 30 * 1000; // 30 seconds, to allow for very slow machines

        using Process p = CreateProcessPortable(RemotelyInvokable.WriteLineReadLine);
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardOutput = true;
        using var mre = new ManualResetEventSlim(false);

        int linesReceived = 0;
        p.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                linesReceived++;

                if (e.Data == expectedSignal)
                {
                    mre.Set();
                }
            }
        };

        p.Start();
        p.BeginOutputReadLine();

        Assert.True(mre.Wait(timeout));
        Assert.Equal(1, linesReceived);

        // Wait a little bit to make sure process didn't exit on itself
        Thread.Sleep(1);
        Assert.False(p.HasExited, "Process has prematurely exited");

        using (StreamWriter writer = p.StandardInput)
        {
            writer.WriteLine(successResponse);
        }

        using (var cts = new CancellationTokenSource(timeout))
        {
            await p.WaitForExitAsync(cts.Token);
            Assert.True(p.HasExited, "Process has not exited");
        }
        Assert.Equal(SuccessExitCode, p.ExitCode);
    }

    [Fact]
    public async Task WaitForExitAsync_AfterProcessExit_ShouldConsumeOutputDataReceived()
    {
        const string message = "test";
        using Process p = CreateProcessPortable(RemotelyInvokable.Echo, message);

        int linesReceived = 0;
        p.OutputDataReceived += (_, e) => { if (e.Data is not null) linesReceived++; };
        p.StartInfo.RedirectStandardOutput = true;

        Assert.True(p.Start());

        // Give time for the process (cmd) to terminate
        while (!p.HasExited)
        {
            Thread.Sleep(20);
        }

        p.BeginOutputReadLine();
        await p.WaitForExitAsync();

        Assert.Equal(1, linesReceived);
    }

    [Fact]
    public async Task WaitAsyncChain()
    {
        Process root = CreateProcess(async () =>
        {
            Process child1 = CreateProcess(async () =>
            {
                Process child2 = CreateProcess(async () =>
                {
                    Process child3 = CreateProcess(RemotelyInvokable.Success);
                    child3.Start();
                    using (var cts = new CancellationTokenSource(WaitInMS))
                    {
                        await child3.WaitForExitAsync(cts.Token);
                        Assert.True(child3.HasExited);
                    }

                    return child3.ExitCode;
                });
                child2.Start();
                using (var cts = new CancellationTokenSource(WaitInMS))
                {
                    await child2.WaitForExitAsync(cts.Token);
                    Assert.True(child2.HasExited);
                }

                return child2.ExitCode;
            });
            child1.Start();
            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await child1.WaitForExitAsync(cts.Token);
                Assert.True(child1.HasExited);
            }

            return child1.ExitCode;
        });
        root.Start();
        using (var cts = new CancellationTokenSource(WaitInMS))
        {
            await root.WaitForExitAsync(cts.Token);
            Assert.True(root.HasExited);
        }
        Assert.Equal(RemoteExecutor.SuccessExitCode, root.ExitCode);
    }

    [Fact]
    public async Task WaitAsyncForSelfTerminatingChild()
    {
        Process child = CreateProcessPortable(RemotelyInvokable.SelfTerminate);
        child.Start();
        using (var cts = new CancellationTokenSource(WaitInMS))
        {
            await child.WaitForExitAsync(cts.Token);
            Assert.True(child.HasExited);
        }
        Assert.NotEqual(RemoteExecutor.SuccessExitCode, child.ExitCode);
    }

    [Fact]
    public async Task WaitAsyncForProcess()
    {
        Process p = CreateDefaultProcess();

        Task processTask = p.WaitForExitAsync();
        Assert.False(p.HasExited);
        Assert.False(processTask.IsCompleted);

        p.Kill();
        await processTask;

        Assert.True(p.HasExited);
    }

    [Fact]
    public async Task WaitForExitAsync_NotDirected_ThrowsInvalidOperationException()
    {
        var process = new Process();
        await Assert.ThrowsAsync<InvalidOperationException>(() => process.WaitForExitAsync());
    }
}