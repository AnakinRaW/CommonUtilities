using System;
using System.ComponentModel;
using System.Diagnostics;
using Vanara.PInvoke;

namespace AnakinRaW.CommonUtilities;

internal static class ProcessElevationWindows
{
    internal static bool IsProcessElevated()
    {
        using var processToken = OpenProcessToken(Process.GetCurrentProcess().Handle);
        try
        {
            var elevation = AdvApi32.GetTokenInformation<AdvApi32.TOKEN_ELEVATION>(processToken, AdvApi32.TOKEN_INFORMATION_CLASS.TokenElevation);
            return elevation.TokenIsElevated;
        }
        catch (Exception e) when (e.HResult == Win32Error.ERROR_INVALID_PARAMETER)
        {
            return false;
        }
    }

    private static AdvApi32.SafeHTOKEN OpenProcessToken(IntPtr process)
    {
        if (!AdvApi32.OpenProcessToken(process, AdvApi32.TokenAccess.TOKEN_QUERY, out var handle))
            throw new Win32Exception();
        return handle;
    }
}