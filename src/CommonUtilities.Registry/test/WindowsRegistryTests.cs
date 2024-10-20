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

    protected override IRegistry CreateRegistry()
    {
        return new WindowsRegistry();
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

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void CreateSubkey_MaxKeyLengthOnWindows()
    {
        // Should throw if key length above 255 characters
        const int maxValueNameLength = 255;
        AssertExtensions.Throws<ArgumentException>("name", null,
            () => TestRegistryKey.CreateSubKey(new string('a', maxValueNameLength + 1)));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void SetValue_MaxNameLengthOnWindows()
    {
        // Should throw if key length above 255 characters but prior to V4, the limit is 16383
        const int maxValueNameLength = 16383;
        AssertExtensions.Throws<ArgumentException>("name", null, () => TestRegistryKey.SetValue(new string('a', maxValueNameLength + 1), 5));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void OpenSubkey_MaxKeyLengthOn_Windows()
    {
        // Should throw if subkey name greater than 255 chars
        AssertExtensions.Throws<ArgumentException>("name", null, () => TestRegistryKey.GetKey(new string('a', 256)));
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public void SetValue_InvalidDataTypes_Windows()
    {
        // Should throw if passed value is array with uninitialized elements
        AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.SetValue("StringArr", value: new string[1]));

        // Should throw because only String[] (REG_MULTI_SZ) and byte[] (REG_BINARY) are supported.
        // RegistryKey.SetValue does not support arrays of type UInt32[].
        AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.SetValue("IntArray", value: new[] { 1, 2, 3 }));
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