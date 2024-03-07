using System;
using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities.FileSystem.Windows.NativeMethods;

internal static class Kernel32
{
    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, MoveFile dwFlags);

    [Flags]
    public enum MoveFile : uint
    {
        ReplaceExisting = 0x00000001,
        CopyAllowed = 0x00000002,
        DelayUntilReboot = 0x00000004,
        WriteThrough = 0x00000008,
        CreateHardlink = 0x00000010,
        FailIfNotTrackable = 0x00000020
    }
}