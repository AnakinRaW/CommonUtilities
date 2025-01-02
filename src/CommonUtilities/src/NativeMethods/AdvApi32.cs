using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities.NativeMethods;

internal static class AdvApi32
{
    [Flags]
    public enum TokenAccess : uint
    {
        Query = 0x0008,
    }

    private enum TOKEN_INFORMATION_CLASS
    {
        TokenElevation = 20,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_ELEVATION
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool TokenIsElevated;
    }

    public static TOKEN_ELEVATION GetTokenElevation(IntPtr hToken)
    {
        var tokenSize = Marshal.SizeOf<TOKEN_ELEVATION>();
        var elevateTokenPtr = Marshal.AllocHGlobal(tokenSize);

        if (!GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenElevation, elevateTokenPtr, tokenSize, out _))
            throw new Win32Exception();

        var elevateToken = Marshal.PtrToStructure<TOKEN_ELEVATION>(elevateTokenPtr);
        return elevateToken;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

    [DllImport("advapi32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenProcessToken(IntPtr ProcessHandle, TokenAccess DesiredAccess, out IntPtr TokenHandle);
}