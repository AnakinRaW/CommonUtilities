using System;
using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities.FileSystem;

internal static class ThrowHelper
{
    public static void ThrowIfNotWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Only available on Windows.");
    }
}