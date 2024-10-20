using System;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void OpenSubKey_NegativeTests()
    {
        // Should throw if passed subkey name is null
        Assert.Throws<ArgumentNullException>(() => TestRegistryKey.GetKey(name: null));

        // OpenSubKey should be read only by default
        const string name = "FooBar";
        TestRegistryKey.SetValue(name, 42);
        using (var rk = Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).GetKey(TestRegistryKeyName))
        {
            Assert.Throws<UnauthorizedAccessException>(() => rk.CreateSubKey(name));
            Assert.Throws<UnauthorizedAccessException>(() => rk.SetValue(name, "String"));
            Assert.Throws<UnauthorizedAccessException>(() => rk.GetValueOrSetDefault("other", "String", out _));
            Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(name));
            Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteKey(name, false));
            Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteKey(name, true));
        }

        // Should throw if RegistryKey closed
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.GetKey(TestRegistryKeyName);
            TestRegistryKey.GetKey(TestRegistryKeyName, true);
        });
    }

    [Fact]
    public void OpenSubKey_Test()
    {
        using var created = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        using var subkey = TestRegistryKey.GetKey(TestRegistryKeyName);
        Assert.NotNull(subkey);
        Assert.Single(TestRegistryKey.GetSubKeyNames());

        TestRegistryKey.DeleteKey(TestRegistryKeyName, false);
        using var subkey2 = TestRegistryKey.GetKey(TestRegistryKeyName);
        Assert.Null(subkey2);
        Assert.Empty(TestRegistryKey.GetSubKeyNames());
    }

    [Theory]
    [InlineData("")]
    [InlineData("\\")]
    public void OpenSubKey_TestSelf(string selfString)
    {
        var expectedName = TestRegistryKey.Name + @"\";

        var self = TestRegistryKey.GetKey(selfString, true);
        Assert.Equal(expectedName, self.Name);

        // Additional tests
        Assert.Empty(TestRegistryKey.GetSubKeyNames());
        // Set the instance with the odd name
        self.SetValue("value", 123);
        // Check on the original instance
        Assert.Equal(123, (int)TestRegistryKey.GetValue("value"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("\\")]
    public void OpenSubKey_TestSelf_MultipleSelfReferencesAppendTrailingSlashes(string selfString)
    {
        var current = TestRegistryKey;
        for (var i = 1; i <= 10; i++)
        {
            current = current.GetKey(selfString);
            Assert.NotNull(current);
            Assert.Equal(TestRegistryKey.Name + new string('\\', i), current.Name);
        }
    }


    [Fact]
    public void OpenSubKeyTest1()
    {
        // [] Should have write rights when true is passed
        const int testValue = 32;
        using var rk = TestRegistryKey.GetKey("", true);
        rk.CreateSubKey(TestRegistryKeyName).Dispose();
        rk.SetValue(TestRegistryKeyName, testValue);

        using var subkey = rk.GetKey(TestRegistryKeyName);
        Assert.NotNull(subkey);
        Assert.Equal(testValue, (int)rk.GetValue(TestRegistryKeyName));
    }

    [Fact]
    public void OpenSubKey_Test2()
    {
        string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x).ToArray();
        foreach (var subKeyName in subKeyNames)
        {
            TestRegistryKey.CreateSubKey(subKeyName).Dispose();
        }

        Assert.Equal(subKeyNames.Length, TestRegistryKey.GetSubKeyNames().Length);
        Assert.Equal(subKeyNames, TestRegistryKey.GetSubKeyNames());
    }

    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void OpenSubKey_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
        Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.GetKey(subKeyName));

    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void OpenSubKey_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
        Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.GetKey(subKeyName));

    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void OpenSubKey_Writable_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
        Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.GetKey(subKeyName, true));
    
    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void OpenSubKey_Writable_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
        Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.GetKey(subKeyName, true));

}