using System;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void DeleteKey_NegativeTests()
    {
        const string name = "Test";

        // Should throw if passed subkey name is null
        Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteKey(null!, false));

        // Should throw if subkey has child subkeys
        using (var rk = TestRegistryKey.CreateSubKey(name))
        {
            using var subkey = rk!.CreateSubKey(name);
            Assert.Throws<InvalidOperationException>(() => TestRegistryKey.DeleteKey(name, false));
        }

        // Should throw because RegistryKey is readonly
        using (var rk = TestRegistryKey.OpenSubKey(string.Empty, writable: false))
        {
            Assert.Throws<UnauthorizedAccessException>(() => rk!.DeleteKey(name, false));
        }

        // Should throw if RegistryKey is closed
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.DeleteKey(name, false);
        });
    }

    [Fact]
    public void DeleteSubKey_Test()
    {
        Assert.Empty(TestRegistryKey.GetSubKeyNames());

        using var subkey = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        Assert.NotNull(subkey);
        Assert.Single(TestRegistryKey.GetSubKeyNames());

        TestRegistryKey.DeleteKey(TestRegistryKeyName, false);
        Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        Assert.Empty(TestRegistryKey.GetSubKeyNames());
    }

    [Fact]
    public void DeleteSubKey_Test2()
    {
        var subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x).ToArray();
        foreach (var subKeyName in subKeyNames) 
            TestRegistryKey.CreateSubKey(subKeyName)!.Dispose();

        Assert.Equal(subKeyNames, TestRegistryKey.GetSubKeyNames());
        foreach (var subKeyName in subKeyNames)
        {
            TestRegistryKey.DeleteKey(subKeyName, false);
            Assert.Null(TestRegistryKey.OpenSubKey(subKeyName));
        }
    }

    [Fact]
    public void DeleteSubKey_ShouldNotDeleteOthers()
    {
        Assert.Empty(TestRegistryKey.GetSubKeyNames());

        using var thisKey = TestRegistryKey.CreateSubKey("this");
        using var thatKey = TestRegistryKey.CreateSubKey("that");

        Assert.NotNull(thisKey);
        Assert.NotNull(thatKey);

        Assert.Equal(2, TestRegistryKey.GetSubKeyNames().Length);

        TestRegistryKey.DeleteKey("this", false);
       
        Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        Assert.Single(TestRegistryKey.GetSubKeyNames());
    }

    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void DeleteKey_KeyExists_DoNotThrow_KeyDeleted(string expected, string subkeyName) =>
        Verify_DeleteSubKey_KeyExists_KeyDeleted(expected, () => TestRegistryKey.DeleteKey(subkeyName, false));

    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void DeleteKey_KeyDoesNotExists_DoNotThrow_DoesNotThrow(string expected, string subkeyName) =>
        Verify_DeleteSubKey_KeyDoesNotExists_DoesNotThrow(expected, () => TestRegistryKey.DeleteKey(subkeyName, false));

}