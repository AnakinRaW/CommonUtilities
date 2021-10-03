using System;
using Microsoft.Win32;

namespace Sklavenwalker.CommonUtilities.Registry.Windows
{
    /// <summary>
    /// Windows specific Registry implementation of <see cref="IRegistry"/>
    /// </summary>
    public class WindowsRegistry : IRegistry
    {
        /// <summary>
        /// Provides a singleton instance for a Windows Registry.
        /// </summary>
        public static readonly IRegistry Default = new WindowsRegistry();

        /// <inheritdoc/>
        public IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view)
        {
            if (view == RegistryView.DefaultOperatingSystem)
                view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
            return new WindowsRegistryKey(RegistryKey.OpenBaseKey(WindowsRegistryKey.ConvertHive(hive), WindowsRegistryKey.ConvertView(view)));
        }
    }
}
