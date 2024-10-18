using System;
using System.Runtime.InteropServices;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Win32;
using Xunit;
#if NET
using System.Runtime.Versioning;
#endif

namespace AnakinRaW.CommonUtilities.Registry.Test;

#if NET
[SupportedOSPlatform("windows")]
#endif
public class WindowsRegistryKeyTest : RegistryTestBase
{
    private RegistryKey _wRegistryKey;
    private IDisposable _keyToDispose;

    protected override IRegistry CreateRegistry()
    {
        var registry = new WindowsRegistry();
        Assert.False(registry.IsCaseSensitive);
        return registry;
    }

    protected override string SubKeyPath => @"SOFTWARE\CommonUtilities.Registry.Windows.Test";

    protected override IRegistryKey CreateRegistryWithKey()
    {
        var key = base.CreateRegistryWithKey();
        _wRegistryKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64)
            .OpenSubKey(SubKeyPath)!;
        _keyToDispose = key;
        return key;
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Integration_HasPath()
    {
        var key = CreateRegistryWithKey();
        Assert.True(key.HasPath(""));
        Assert.NotNull(_wRegistryKey);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Integration_CrudValues()
    {
        var key = CreateRegistryWithKey();

        key.SetValue("Test", true);
        Assert.NotNull(_wRegistryKey.GetValue("Test"));

        key.DeleteValue("Test");
        Assert.Null(_wRegistryKey.GetValue("Test"));

        key.CreateSubKey("Sub\\SubSub");
        var deleted = key.DeleteKey("Sub", false);
        Assert.False(deleted);
        Assert.NotNull(_wRegistryKey.OpenSubKey("Sub\\SubSub"));

        deleted = key.DeleteKey("Sub", true);
        Assert.True(deleted);
        Assert.Null(_wRegistryKey.OpenSubKey("Sub\\SubSub"));

    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void Integration_CrudKeys()
    {
        var key = CreateRegistryWithKey();

        var sub = key.CreateSubKey("Sub")!;
        var w_sub = _wRegistryKey.OpenSubKey("Sub");
        Assert.NotNull(w_sub);

        sub.SetValue("Test", true);
        Assert.NotNull(w_sub.GetValue("Test"));

        key.DeleteValue("Test", "Sub");
        Assert.Null(w_sub.GetValue("Test"));

        var deleted = key.DeleteKey("Sub", false);
        Assert.True(deleted);
        Assert.Null(_wRegistryKey.OpenSubKey("Sub"));
    }
    
    public override void Dispose()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var wBase = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Default);
        wBase.DeleteSubKeyTree(SubKeyPath!);
        _keyToDispose.Dispose();
    }
}