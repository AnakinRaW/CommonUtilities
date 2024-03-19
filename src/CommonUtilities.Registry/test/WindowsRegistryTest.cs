using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Win32;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

#if NET
[SupportedOSPlatform("windows")]
#endif
public class WindowsRegistryKeyTest : IDisposable
{
    private const string SubKey = @"SOFTWARE\CommonUtilities.Registry.Windows.Test";
    private readonly IRegistryKey _registryKey;

    private readonly RegistryKey _wRegistryKey;

    public WindowsRegistryKeyTest()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var baseKey = new WindowsRegistry().OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        _registryKey = baseKey.CreateSubKey(SubKey)!;
        _wRegistryKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Default)
            .OpenSubKey(SubKey)!;
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void TestExists()
    {
        Assert.True(_registryKey.HasPath(""));
        Assert.NotNull(_wRegistryKey);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void TestCrud()
    {
        _registryKey.SetValue("Test", true);
        Assert.NotNull(_wRegistryKey.GetValue("Test"));

        _registryKey.DeleteValue("Test");
        Assert.Null(_wRegistryKey.GetValue("Test"));

        _registryKey.CreateSubKey("Sub\\SubSub");
        var deleted = _registryKey.DeleteKey("Sub", false);
        Assert.False(deleted);
        Assert.NotNull(_wRegistryKey.OpenSubKey("Sub\\SubSub"));

        deleted = _registryKey.DeleteKey("Sub", true);
        Assert.True(deleted);
        Assert.Null(_wRegistryKey.OpenSubKey("Sub\\SubSub"));

    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void TestCrudSub()
    {
        var sub = _registryKey.CreateSubKey("Sub")!;
        var w_sub = _wRegistryKey.OpenSubKey("Sub");
        Assert.NotNull(w_sub);

        sub.SetValue("Test", true);
        Assert.NotNull(w_sub.GetValue("Test"));

        _registryKey.DeleteValue("Test", "Sub");
        Assert.Null(w_sub.GetValue("Test"));

        var deleted = _registryKey.DeleteKey("Sub", false);
        Assert.True(deleted);
        Assert.Null(_wRegistryKey.OpenSubKey("Sub"));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void TestSelfDelete()
    {
        _registryKey.DeleteValue("Test");
        Assert.Null(_wRegistryKey.GetValue("Test"));

        var sub = _registryKey.CreateSubKey("Sub");
        var deleted = sub!.DeleteKey("", true);
        Assert.True(deleted);
        Assert.Null(_wRegistryKey.OpenSubKey("Sub"));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void TestDataTypes()
    {
        _registryKey.SetValue("TestEnum", 1);
        _registryKey.GetValue("TestEnum", out int oi);
        var i = Assert.IsType<int>(oi);
        Assert.Equal(1, i);

        _registryKey.SetValue("TestEnum", 1ul);
        _registryKey.GetValue("TestEnum", out ulong oul);
        var ul = Assert.IsType<ulong>(oul);
        Assert.Equal(1ul, ul);

        _registryKey.SetValue("TestBool", true);
        _registryKey.GetValue("TestBool", out bool ob);
        var b = Assert.IsType<bool>(ob);
        Assert.True(b);

        _registryKey.SetValue("TestEnum", MyEnum.B);
        _registryKey.GetValue("TestEnum", out MyEnum oe);
        var e = Assert.IsType<MyEnum>(oe);
        Assert.Equal(MyEnum.B, e);
    }

    public void Dispose()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var wBase = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Default);
        wBase.DeleteSubKeyTree(SubKey);
        _registryKey.Dispose();
    }

    private enum MyEnum
    {
        A,
        B
    }
}