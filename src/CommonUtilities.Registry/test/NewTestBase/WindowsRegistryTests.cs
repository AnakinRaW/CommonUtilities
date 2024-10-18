using AnakinRaW.CommonUtilities.Registry.Windows;
using Microsoft.Win32;
using Xunit;
#if NET
using System.Runtime.Versioning;
#endif

namespace AnakinRaW.CommonUtilities.Registry.Test;

#if Windows

#if NET
[SupportedOSPlatform("windows")]
#endif
public class WindowsRegistryTests : RegistryTestsBase
{
    protected override IRegistry CreateRegistry()
    {
        var registry = new WindowsRegistry();
        Assert.False(registry.IsCaseSensitive);
        return registry;
    }

    protected override void RemoveKeyIfExists(string keyName)
    {
        var rk = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Default);
        using var subkey = rk.OpenSubKey(keyName);
        if (subkey != null)
        {
            rk.DeleteSubKeyTree(keyName);
            Assert.Null(rk.OpenSubKey(keyName));
        }
    }
}

#endif