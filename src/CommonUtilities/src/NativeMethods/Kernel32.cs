using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace AnakinRaW.CommonUtilities.NativeMethods;

internal static class Kernel32
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);

    internal static string? GetModuleFileName(IntPtr hModule)
    {
        var builder = new StringBuilder(260);

        uint length;
        while ((length = GetModuleFileName(hModule, builder, (uint)builder.Capacity)) >= builder.Capacity) 
            builder.EnsureCapacity((int)length);

        if (length == 0)
            throw new Win32Exception();

        builder.Length = (int)length;
        return builder.ToString();
    }

    [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
    public static extern uint GetModuleFileName(IntPtr hModule, StringBuilder buffer, uint length);
}