using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;

namespace AnakinRaW.CommonUtilities.Testing.IO;

public static class RemotelyInvokable
{
    public static readonly int SuccessExitCode = 42;

    public static int Sleep(string duration, string callerName)
    {
        _ = callerName; // argument ignored, for debugging purposes
        Thread.Sleep(int.Parse(duration));
        return SuccessExitCode;
    }

    public static int Echo(string value)
    {
        Console.WriteLine(value);
        return SuccessExitCode;
    }

    public static int WriteLineReadLine()
    {
        Console.WriteLine("Signal");
        string line = Console.ReadLine();
        return line == "Success" ? SuccessExitCode : SuccessExitCode + 1;
    }

    public static int SelfTerminate()
    {
        Process.GetCurrentProcess().Kill();
        throw new Exception();
    }
}

public class ProcessTestBase
{
    public static readonly int SuccessExitCode = 42;

    protected static readonly int WaitInMS = 30 * 1000 * PlatformDetection.SlowRuntimeTimeoutModifier;
    protected Process _process;
    protected readonly List<Process> Processes = new();

    protected Process CreateDefaultProcess([CallerMemberName] string callerName = null)
    {
        Assert.Null(_process);
        _process = CreateProcessLong(callerName);
        _process.Start();
        AddProcessForDispose(_process);
        return _process;
    }

    protected Process CreateProcessLong([CallerMemberName] string callerName = null)
    {
        return CreateSleepProcess(WaitInMS, callerName);
    }

    protected Process CreateSleepProcess(int durationMs, [CallerMemberName] string callerName = null)
    {
        return CreateProcess(RemotelyInvokable.Sleep, durationMs.ToString(), callerName);
    }

    protected Process CreateProcessPortable(Func<int> func)
    {
        return CreateProcess(func);
    }

    protected Process CreateProcessPortable(Func<string, int> func, string arg)
    {
        return CreateProcess(func, arg);
    }

    public void Dispose(bool disposing)
    {
        // Wait for all started processes to complete
        foreach (Process p in Processes)
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

    protected void AddProcessForDispose(Process p)
    {
        lock (Processes)
        {
            Processes.Add(p);
        }
    }

    protected Process CreateProcess(Func<int> method = null)
    {
        Process p = null;
        using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(method ?? (() => RemoteExecutor.SuccessExitCode), new RemoteInvokeOptions { Start = false }))
        {
            p = handle.Process;
            handle.Process = null;
        }
        AddProcessForDispose(p);
        return p;
    }

    protected Process CreateProcess(Func<Task<int>> method)
    {
        Process p = null;
        using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(method, new RemoteInvokeOptions { Start = false }))
        {
            p = handle.Process;
            handle.Process = null;
        }
        AddProcessForDispose(p);
        return p;
    }

    protected Process CreateProcess(Func<string, int> method, string arg, bool autoDispose = true)
    {
        Process p = null;
        using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(method, arg, new RemoteInvokeOptions { Start = false }))
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

    protected Process CreateProcess(Func<string, string, int> method, string arg1, string arg2, bool autoDispose = true)
    {
        Process p = null;
        using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(method, arg1, arg2, new RemoteInvokeOptions { Start = false }))
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

    protected Process CreateProcess(Func<string, Task<int>> method, string arg)
    {
        Process p = null;
        using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(method, arg, new RemoteInvokeOptions { Start = false }))
        {
            p = handle.Process;
            handle.Process = null;
        }
        AddProcessForDispose(p);
        return p;
    }

    protected void StartSleepKillWait(Process p)
    {
        p.Start();
        Thread.Sleep(200);
        KillWait(p);
    }

    protected void KillWait(Process p)
    {
        p.Kill();
        Assert.True(p.WaitForExit(WaitInMS));
        p.WaitForExit(); // wait for event handlers to complete
    }
}

public static class PlatformDetection
{
    private static readonly Lazy<bool> s_isReleaseRuntime = new(() => AssemblyConfigurationEquals("Release"));
    private static readonly Lazy<bool> s_isDebugRuntime = new(() => AssemblyConfigurationEquals("Debug"));

    public static bool IsReleaseRuntime => s_isReleaseRuntime.Value;
    public static bool IsDebugRuntime => s_isDebugRuntime.Value;

    public static int SlowRuntimeTimeoutModifier
    {
        get
        {
            if (IsReleaseRuntime)
                return 1;
            return IsDebugRuntime ? 5 : 1;
        }
    }

    private static bool AssemblyConfigurationEquals(string configuration)
    {
        AssemblyConfigurationAttribute assemblyConfigurationAttribute = typeof(string).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();

        return assemblyConfigurationAttribute != null &&
               string.Equals(assemblyConfigurationAttribute.Configuration, configuration, StringComparison.InvariantCulture);
    }
}