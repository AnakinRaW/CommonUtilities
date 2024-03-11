using System;
using System.ComponentModel;
using System.Diagnostics;
using AnakinRaW.CommonUtilities.NativeMethods;
using AdvApi32 = AnakinRaW.CommonUtilities.NativeMethods.AdvApi32;

namespace AnakinRaW.CommonUtilities;

internal static class ProcessElevationWindows
{
    public const uint ErrorInvalidParameter = 0x00000057;

    internal static bool IsProcessElevated()
    {
        var processToken = OpenProcessToken(Process.GetCurrentProcess().Handle);
        try
        {
            try
            {
                var elevation = AdvApi32.GetTokenElevation(processToken);
                return elevation.TokenIsElevated;
            }
            catch (Exception e) when (e.HResult == ErrorInvalidParameter)
            {
                return false;
            }
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