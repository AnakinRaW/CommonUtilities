using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

// Test Suite based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Win32.Registry/tests
public partial class RegistryTestsBase
{
    [Fact]
    public void GetValueNames_ShouldThrowIfDisposed()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.GetValueNames();
        });
    }

    [Fact]
    public void GetValueNames_ShouldThrowIfRegistryKeyDeleted()
    {
        Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).DeleteKey(TestRegistryKeyName, true);
        Assert.Throws<IOException>(() => TestRegistryKey.GetValueNames());
    }

    [Fact]
    public void GetValueNames_Test()
    {
        // [] Add several values and get the values then check the names
        Assert.Empty(TestRegistryKey.GetValueNames());

        string[] expected = [TestRegistryKeyName];
        foreach (var valueName in expected) 
            TestRegistryKey.SetValue(valueName, 5);

        Assert.Equal(expected, TestRegistryKey.GetValueNames());

        TestRegistryKey.DeleteValue(TestRegistryKeyName);
        Assert.Empty(TestRegistryKey.GetValueNames());
    }

    [Fact]
    public void GetValueNames_Test2()
    {
        foreach (var testCase in TestData.TestValueTypes) 
            TestRegistryKey.SetValue(testCase[0].ToString(), testCase[1]);

        var expected = TestData.TestValueTypes.Select(x => x[0].ToString()).ToArray();
        Assert.Equal(expected, TestRegistryKey.GetValueNames());
    }
}