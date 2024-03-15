using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace AnakinRaW.CommonUtilities.Registry.Windows;

/// <summary>
/// Windows specific Registry implementation of <see cref="IRegistry"/>
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRegistry : IRegistry
{
    /// <summary>
    /// Provides a singleton instance for a Windows Registry.
    /// </summary>
    public static readonly IRegistry Default = new WindowsRegistry();

    /// <inheritdoc/>
    public IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Registry is not supported on this platform.");

        if (view == RegistryView.DefaultOperatingSystem)
            view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
        return new WindowsRegistryKey(RegistryKey.OpenBaseKey(WindowsRegistryKey.ConvertHive(hive), WindowsRegistryKey.ConvertView(view)));
    }
}