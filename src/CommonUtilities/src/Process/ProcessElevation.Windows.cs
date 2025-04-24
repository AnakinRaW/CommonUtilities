using AnakinRaW.CommonUtilities.NativeMethods;
using System;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using AdvApi32 = AnakinRaW.CommonUtilities.NativeMethods.AdvApi32;

namespace AnakinRaW.CommonUtilities;

internal static class ProcessElevationWindows
{
    public const uint ErrorInvalidParameter = 0x00000057;

    internal static bool IsProcessElevated(int processId)
    {
        using var processHandle = 
            new SafeFileHandle(Kernel32.OpenProcess(Kernel32.ProcessAccessRights.ProcessQueryLimitedInformation, false, (uint)processId), true);
        
        var processToken = OpenProcessToken(processHandle.DangerousGetHandle());
        try
        {
            var elevation = AdvApi32.GetTokenElevation(processToken);
            return elevation.TokenIsElevated;
        }
        catch (Exception e) when (e.HResult == ErrorInvalidParameter)
        {
            return false;
        }
        finally
        {
            if (processToken !=  IntPtr.Zero)
                Kernel32.CloseHandle(processToken);
        }
    }

    private static IntPtr OpenProcessToken(IntPtr process)
    {
        if (!AdvApi32.OpenProcessToken(process, AdvApi32.TokenAccess.Query, out var handle))
            throw new Win32Exception();
        return handle;
    }
}