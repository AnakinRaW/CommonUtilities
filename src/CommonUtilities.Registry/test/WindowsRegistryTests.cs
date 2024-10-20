using AnakinRaW.CommonUtilities.Registry.Windows;
using Microsoft.Win32;
using Xunit;
using System;
using AnakinRaW.CommonUtilities.Testing;

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
    public override bool IsCaseSensitive => false;
    public override bool HasPathLimits => true;
    public override bool HasTypeLimits => true;

    protected override IRegistry CreateRegistry()
    {
        return new WindowsRegistry();
    }

    protected override void RemoveKeyIfExists(string keyName)
    {
        var rk = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Default);
        using var subKey = rk.OpenSubKey(keyName);
        if (subKey != null)
        {
            rk.DeleteSubKeyTree(keyName);
            Assert.Null(rk.OpenSubKey(keyName));
        }
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Integration_HasPath()
    {
        var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(TestRegistryKeyName);
        Assert.NotNull(key);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Integration_CrudKeyValues()
    {
        var wRegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(TestRegistryKeyName);

        TestRegistryKey.SetValue("Test", true);
        Assert.NotNull(wRegistryKey.GetValue("Test"));

        TestRegistryKey.DeleteValue("Test");
        Assert.Null(wRegistryKey.GetValue("Test"));

        TestRegistryKey.CreateSubKey("Sub\\SubSub\\SubSubSub");
        TestRegistryKey.DeleteKey("Sub\\SubSub\\SubSubSub", false);
        Assert.Null(wRegistryKey.OpenSubKey("Sub\\SubSub\\SubSubSub"));
        
        TestRegistryKey.DeleteKey("Sub", true);
        Assert.Null(wRegistryKey.OpenSubKey("Sub\\SubSub"));
    }
}

#endif