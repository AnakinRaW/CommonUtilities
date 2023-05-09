using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities;

internal static class ProcessElevationLinux
{
    // ReSharper disable once IdentifierTypo
    [DllImport("libc", SetLastError = true)]
    private static extern uint getuid();

    internal static bool IsElevated()
    {
        return (int)getuid() == 0;
    }
}
