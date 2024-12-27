using System.Runtime.InteropServices;
using AnakinRaW.CommonUtilities.Registry.Extensions;
using Microsoft.Win32.SafeHandles;

namespace AnakinRaW.CommonUtilities.Registry.Windows;

internal static class Advapi32
{
    internal const RegistryChangeNotificationFilters RegNotifyThreadAgnostic = (RegistryChangeNotificationFilters)0x10000000L;

    [DllImport("Advapi32.dll", ExactSpelling = true, SetLastError = true)]
    internal static extern int RegNotifyChangeKeyValue(
        SafeRegistryHandle hKey,
        [MarshalAs(UnmanagedType.Bool)] bool watchSubtree,
        RegistryChangeNotificationFilters notifyFilter,
        SafeWaitHandle hEvent,
        [MarshalAs(UnmanagedType.Bool)] bool asynchronous);
}