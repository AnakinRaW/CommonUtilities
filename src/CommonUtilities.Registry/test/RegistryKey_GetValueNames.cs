using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

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
        Assert.Equal(expected: 0, actual: TestRegistryKey.GetValueNames().Length);

        string[] expected = [TestRegistryKeyName];
        foreach (var valueName in expected) 
            TestRegistryKey.SetValue(valueName, 5);

        Assert.Equal(expected, TestRegistryKey.GetValueNames());

        TestRegistryKey.DeleteValue(TestRegistryKeyName);
        Assert.Equal(expected: 0, actual: TestRegistryKey.GetValueNames().Length);
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