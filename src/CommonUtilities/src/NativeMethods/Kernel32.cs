using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace AnakinRaW.CommonUtilities.NativeMethods;

internal static class Kernel32
{
    internal enum ProcessAccessRights
    {
        ProcessTerminate = 1,
        ProcessCreateThread = 2,
        ProcessSetSessionId = 4,
        ProcessVmOperation = 8,
        ProcessVmRead = 16,
        ProcessVmWrite = 32,
        ProcessDupHandle = 64,
        ProcessCreateProcess = 128,
        ProcessSetQuota = 256,
        ProcessSetInformation = 512,
        ProcessQueryInformation = 1024,
        ProcessSuspendResume = 2048,
        ProcessQueryLimitedInformation = 4096,
        ProcessSetLimitedInformation = 8192,
        ProcessAllAccess = 2097151,
        ProcessDelete = 65536,
        ProcessReadControl = 131072,
        ProcessWriteDac = 262144,
        ProcessWriteOwner = 524288,
        ProcessSynchronize = 1048576,
        ProcessStandardRightsRequired = ProcessWriteOwner | ProcessWriteDac | ProcessReadControl | ProcessDelete
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);

    internal static string GetModuleFileName(IntPtr hModule)
    {
        // 260 is the MaxShortPath length
        var builder = new StringBuilder(260);

        uint length;
        while ((length = GetModuleFileName(hModule, builder, (uint)builder.Capacity)) >= builder.Capacity) 
            builder.EnsureCapacity((int)length);

        if (length == 0)
            throw new Win32Exception();

        builder.Length = (int)length;
        return builder.ToString();
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
    private static extern uint GetModuleFileName(IntPtr hModule, StringBuilder buffer, uint length);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("KERNEL32.dll", SetLastError = true)]
    internal static extern IntPtr OpenProcess(ProcessAccessRights dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
}