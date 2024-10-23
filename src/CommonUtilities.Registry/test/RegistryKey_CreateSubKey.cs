using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

// Test Suite based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Win32.Registry/tests
public partial class RegistryTestsBase
{
    [Fact]
    public void CreateSubkey_NegativeTests()
    {
        // Should throw if passed subkey name is null
        Assert.Throws<ArgumentNullException>(() => TestRegistryKey.CreateSubKey(null!));

        // Should throw if RegistryKey is readonly
        const string name = "FooBar";
        TestRegistryKey.SetValue(name, 42);
        using (var rk = CreateRegistry().OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).CreateSubKey(TestRegistryKeyName, false))
        {
            Assert.Throws<UnauthorizedAccessException>(() => rk.CreateSubKey(name));
            Assert.Throws<UnauthorizedAccessException>(() => rk.SetValue(name, "String"));
            Assert.Throws<UnauthorizedAccessException>(() => rk.GetValueOrSetDefault("other", 123, out _));
            Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(name));
            Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteKey(name, false));
            Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteKey(name, true));
        }

        // Should throw if RegistryKey closed
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("\\")]
    public void CreateSubkey_WithEmptyName_ShouldNotCreateNewSubKey(string selfString)
    {
        var expectedName = TestRegistryKey.Name + @"\";
        using var rk = TestRegistryKey.CreateSubKey(selfString);
        Assert.NotNull(rk);
        Assert.Equal(expectedName, rk.Name);

        // Additional tests
        Assert.Empty(TestRegistryKey.GetSubKeyNames());
        // Set the instance with the odd name
        rk.SetValue("value", 123);
        // Check on the original instance
        Assert.Equal(123, (int)TestRegistryKey.GetValue("value")!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\\")]
    public void CreateSubkey_WithEmptyName_MultipleSelfReferencesAppendTrailingSlashes(string selfString)
    {
        var current = TestRegistryKey;
        for (var i = 1; i <= 10; i++)
        {
            current = current.CreateSubKey(selfString);
            Assert.NotNull(current);
            Assert.Equal(TestRegistryKey.Name + new string('\\', i), current.Name);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("\\")]
    public void CreateSubkey_WithEmptyName_ShouldCreateCorrectSubKey(string selfString)
    {
        var expectedName = TestRegistryKey.Name + @"\";
        using var rk = TestRegistryKey.CreateSubKey(selfString);
        Assert.NotNull(rk);
        Assert.Equal(expectedName, rk.Name);
        
        var subKey = rk.CreateSubKey("sub");

        Assert.Single(rk.GetSubKeyNames());
        Assert.Single(TestRegistryKey.GetSubKeyNames());

        
        Assert.Equal(expectedName + @"\" + "sub", subKey!.Name);
    }

    [Fact]
    public void CreateSubKey_AndCheckThatItExists()
    {
        using var created = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        Assert.NotNull(created);

        using var opened = TestRegistryKey.OpenSubKey(TestRegistryKeyName);
        Assert.NotNull(opened);

        Assert.Single(TestRegistryKey.GetSubKeyNames()!);
    }

    [Fact]
    public void CreateSubKey_ShouldOpenExisting()
    {
        // CreateSubKey should open subkey if it already exists
        using var subkey1 = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        Assert.NotNull(subkey1);

        using var subkey2 = TestRegistryKey.OpenSubKey(TestRegistryKeyName);
        Assert.NotNull(subkey2);

        using var subkey3 = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        Assert.NotNull(subkey3);
    }

    [Theory]
    [InlineData("Dyalog APL/W 10.0")]
    [InlineData(@"a\b\c\d\e\f\g\h")]
    [InlineData(@"a\b\c\/d\//e\f\g\h\//\\")]
    public void CreateSubKey_WithName(string subkeyName)
    {
        using var created = TestRegistryKey.CreateSubKey(subkeyName);
        Assert.NotNull(created);

        using var opened = TestRegistryKey.OpenSubKey(subkeyName);
        Assert.NotNull(opened);
    }

    [Fact]
    public void CreateSubKey_DeepTest()
    {
        //[] how deep can we go with this

        var subkeyName = string.Empty;

        // Changed the number of times we repeat str1 from 100 to 30 in response to the Windows OS
        //There is a restriction of 255 characters for the keyname even if it is multikeys. Not worth to pursue as a bug
        // reduced further to allow for WoW64 changes to the string.
        for (var i = 0; i < 25 && subkeyName.Length < 230; i++)
            subkeyName = subkeyName + i + @"\";

        using var created = TestRegistryKey.CreateSubKey(subkeyName);
        Assert.NotNull(created);

        using var opened = TestRegistryKey.OpenSubKey(subkeyName);
        Assert.NotNull(opened);

        //However, we are interested in ensuring that there are no buffer overflow issues with a deeply nested keys
        var keys = new List<IRegistryKey>();
        var rk = TestRegistryKey;
        for (var i = 0; i < 3; i++)
        {
            rk = rk.OpenSubKey(subkeyName, true);
            Assert.NotNull(rk);
            keys.Add(rk);

            keys.Add(rk.CreateSubKey(subkeyName)!);
        }

        keys.ForEach(key => key.Dispose());
    }

    [Fact]
    public void CreateWriteableSubkeyAndWrite()
    {
        // [] Vanilla; create a new subkey in read/write mode and write to it
        const string testValueName = "TestValue";
        const string testStringValueName = "TestString";
        const string testStringValue = "Hello World!\u2020\u00FE";
        const int testValue = 42;

        using var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        Assert.NotNull(rk);

        rk.SetValue(testValueName, testValue);
        Assert.Single(rk.GetValueNames());

        rk.SetValue(testStringValueName, testStringValue);
        Assert.Equal(2, rk.GetValueNames().Length);

        Assert.Equal(testValue, rk.GetValue(testValueName));
        Assert.Equal(testStringValue, rk.GetValue(testStringValueName)!.ToString());
    }

    [Fact]
    public void CreateSbuKey_WithWhitespaceName()
    {
        const string name = "   ";
        TestRegistryKey.CreateSubKey(name);
        Assert.NotNull(TestRegistryKey.OpenSubKey(name));
    }

    [Fact]
    public void CreateSubKey_OnDeletedKeyShouldThrow()
    {
        using var sub = TestRegistryKey.CreateSubKey("sub", writable: true);
        TestRegistryKey.DeleteKey("sub", true);
        Assert.Null(TestRegistryKey.OpenSubKey("sub"));
        Assert.Throws<IOException>(() => sub!.CreateSubKey("subsub"));
    }

    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void CreateSubKey_Writable_KeyExists_OpensKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyExists_OpensKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, writable: true)!);


    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void CreateSubKey_NonWritable_KeyExists_OpensKeyWithFixedUpName(string expected, string subKeyName) =>
        Verify_CreateSubKey_KeyExists_OpensKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, writable: false)!);


    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void CreateSubKey_Writable_KeyDoesNotExist_CreatesKeyWithFixedUpName(string expected, string subKeyName) =>
        Verify_CreateSubKey_KeyDoesNotExist_CreatesKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, writable: true)!);


    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void CreateSubKey_NonWritable_KeyDoesNotExist_CreatesKeyWithFixedUpName(string expected, string subKeyName) =>
        Verify_CreateSubKey_KeyDoesNotExist_CreatesKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, writable: false)!);

}