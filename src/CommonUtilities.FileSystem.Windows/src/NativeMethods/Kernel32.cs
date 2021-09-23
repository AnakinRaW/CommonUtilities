using System.Runtime.InteropServices;

namespace Sklavenwalker.CommonUtilities.FileSystem.Windows.NativeMethods
{
    internal static class Kernel32
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetDriveTypeW(string nDrive);
    }
}