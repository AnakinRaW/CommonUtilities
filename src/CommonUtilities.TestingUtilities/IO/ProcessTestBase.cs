using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;

namespace AnakinRaW.CommonUtilities.Testing.IO;

public class ProcessTestBase
{
    public const int WaitInMS = 5 * 60 * 1000;

    private readonly List<Process> _processes = new();

    protected Process CreateProcessLong([CallerMemberName] string callerName = null)
    {
        return CreateSleepProcess(WaitInMS, callerName);
    }

    protected Process CreateSleepProcess(int durationMs, [CallerMemberName] string callerName = null)
    {
        return CreateProcess(Sleep, durationMs.ToString(), callerName);
    }

    public static int Sleep(string duration, string callerName)
    {
        _ = callerName; // argument ignored, for debugging purposes
        Thread.Sleep(int.Parse(duration));
        return 42;
    }

    private Process CreateProcess(Func<string, string, int> method, string arg1, string arg2, bool autoDispose = true)
    {
        Process p;
        using (var handle = RemoteExecutor.Invoke(method, arg1, arg2, new RemoteInvokeOptions { Start = false }))
        {
            p = handle.Process;
            handle.Process = null;
        }

        if (autoDispose)
        {
            AddProcessForDispose(p);
        }

        return p;
    }

    private void AddProcessForDispose(Process p)
    {
        lock (_processes)
        {
            _processes.Add(p);
        }
    }

    public void Dispose()
    {
        // Wait for all started processes to complete
        foreach (Process p in _processes)
        {
            try
            {
                p.Kill();
                Assert.True(p.WaitForExit(WaitInMS));
                p.WaitForExit(); // wait for event handlers to complete
            }
            catch (InvalidOperationException) { } // in case it was never started
        }
    }
}