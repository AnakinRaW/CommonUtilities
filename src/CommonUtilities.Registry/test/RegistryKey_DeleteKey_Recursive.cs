using System;
using System.Linq;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void DeleteKey_Recursive_NegativeTests()
    {
        const string name = "Test";

        // Should throw if passed subkey name is null
        Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteKey(null!, true));

        // Should throw if target subkey is system subkey and name is empty
        AssertExtensions.Throws<ArgumentException>(null, () => Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
            .DeleteKey(string.Empty, true));

        // Should throw because RegistryKey is readonly
        using (var rk = TestRegistryKey.OpenSubKey(string.Empty, false))
        {
            Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteKey(name, true));
        }

        // Should throw if RegistryKey is closed
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.DeleteKey(name, true);
        });
    }

    [Fact]
    public void DeleteKey_Recursive_ResolvedSelfShouldThrowOnSystemKey()
    {
        // Should throw if target subkey is system subkey and name results in self
        Assert.Throws<UnauthorizedAccessException>(() => Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
            .DeleteKey("\\", true));
    }

    [Fact]
    public void DeleteKey_Recursive_SubkeyMissingTest()
    {
        //Should NOT throw when throwOnMissing is false with subkey missing
        const string name = "Test";
        TestRegistryKey.DeleteKey(name, true);
    }

    [Fact]
    public void DeleteKey_Recursive_SubkeyExistsTests()
    {
        const string subKeyExists = "SubkeyExists";

        using var rk = TestRegistryKey.CreateSubKey(subKeyExists);
        using var a = rk.CreateSubKey("a");
        using var b = rk.CreateSubKey("b");
        TestRegistryKey.DeleteKey(subKeyExists, true);
    }

    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void DeleteKey_Recursive_DoNotThrow_KeyExists_KeyDeleted(string expected, string subKeyName) =>
        Verify_DeleteSubKey_KeyExists_KeyDeleted(expected, () => TestRegistryKey.DeleteKey(subKeyName, true));

    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void DeleteKey_Recursive_DoNotThrow_KeyDoesNotExists_DoesNotThrow(string expected, string subKeyName) =>
        Verify_DeleteSubKey_KeyDoesNotExists_DoesNotThrow(expected, () => TestRegistryKey.DeleteKey(subKeyName, true));

    [Theory]
    [MemberData(nameof(TestRegistrySubKeyNames))]
    public void DeleteSubKeyTree_KeyExists_KeyDeleted(string expected, string subKeyName) =>
        Verify_DeleteSubKey_KeyExists_KeyDeleted(expected, () => TestRegistryKey.DeleteKey(subKeyName, true));

    [Theory]
    [InlineData("")]
    [InlineData("\\")]
    public void DeleteKey_Recursive_SelfDeleteTest(string selfName)
    {
        using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
        {
            using var created = rk.CreateSubKey(TestRegistryKeyName);
            rk.DeleteKey(selfName, true);
        }
        Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
    }

    [Fact]
    public void DeleteKey_Recursive_SelfDeleteWithValuesTest()
    {
        using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
        {
            rk.SetValue("VAL", "Dummy");
            rk.SetValue(null, "Default");
            using var created = rk.CreateSubKey(TestRegistryKeyName);
            created.SetValue("Value", 42);
            rk.DeleteKey("", true);
        }

        Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
    }

    [Fact]
    public void DeleteKey_Recursive_SelfDeleteWithValuesTest_AnotherHandlePresent()
    {
        using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
        {
            rk.SetValue("VAL", "Dummy");
            rk.SetValue(null, "Default");
            using var created = rk.CreateSubKey(TestRegistryKeyName);
            created.SetValue("Value", 42);

            using var rk2 = TestRegistryKey.OpenSubKey(TestRegistryKeyName);
            rk.DeleteKey("", true);
        }

        Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
    }

    [Fact]
    public void DeleteKey_Recursive_Test()
    {
        // Creating new SubKey and deleting it
        using var created = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        using var opened = TestRegistryKey.OpenSubKey(TestRegistryKeyName);
        Assert.NotNull(opened);

        TestRegistryKey.DeleteKey(TestRegistryKeyName, true);
        Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
    }

    [Fact]
    public void DeleteKey_Recursive_Test2()
    {
        // [] Add in multiple subkeys and then delete the root key
        string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x).ToArray();

        using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
        {
            foreach (var subKeyName in subKeyNames)
            {
                using var rk2 = rk.CreateSubKey(subKeyName);
                Assert.NotNull(rk2);

                using var rk3 = rk2.CreateSubKey("Test");
                Assert.NotNull(rk3);
            }

            Assert.Equal(subKeyNames, rk.GetSubKeyNames());
        }

        TestRegistryKey.DeleteKey(TestRegistryKeyName, true);
        Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
    }

    [Fact]
    public void DeleteKey_Recursive_Test3()
    {
        // [] Add in multiple subkeys and then delete the root key
        var subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x).ToArray();

        using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
        {
            foreach (var subKeyName in subKeyNames)
            {
                using var rk2 = rk.CreateSubKey(subKeyName);
                Assert.NotNull(rk2);

                using var rk3 = rk2.CreateSubKey("Test");
                Assert.NotNull(rk3);
            }

            Assert.Equal(subKeyNames, rk.GetSubKeyNames());

            // Add multiple values to the key being deleted
            foreach (var i in Enumerable.Range(1, 9))
            {
                rk.SetValue("STRVAL_" + i, i.ToString());
                rk.SetValue("INTVAL_" + i, i);
            }
        }

        TestRegistryKey.DeleteKey(TestRegistryKeyName, true);
        Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
    }

    [Fact]
    public void DeleteSubKey_Recursive_ShouldNotDeleteOthers()
    {
        // [] Add in multiple subkeys and then delete the root key
        var subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x).ToArray();

        using var other = TestRegistryKey.CreateSubKey("other");
        using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
        {
            foreach (var subKeyName in subKeyNames)
            {
                using var rk2 = rk.CreateSubKey(subKeyName);
                Assert.NotNull(rk2);

                using var rk3 = rk2.CreateSubKey("Test");
                Assert.NotNull(rk3);
            }

            Assert.Equal(subKeyNames, rk.GetSubKeyNames());

            // Add multiple values to the key being deleted
            foreach (var i in Enumerable.Range(1, 9))
            {
                rk.SetValue("STRVAL_" + i, i.ToString());
                rk.SetValue("INTVAL_" + i, i);
            }
        }

        Assert.Equal(2, TestRegistryKey.GetSubKeyNames().Length);

        TestRegistryKey.DeleteKey(TestRegistryKeyName, true);
        Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));

        Assert.Single(TestRegistryKey.GetSubKeyNames());
    }
}