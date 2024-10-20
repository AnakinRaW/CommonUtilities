using System;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void DeleteValue_NegativeTests()
    {
        const string valueName = "TestValue";

        // Should NOT throw because value doesn't exists
        TestRegistryKey.DeleteValue(valueName);

        
        TestRegistryKey.SetValue(valueName, 42);

        // Should throw because RegistryKey is readonly
        using (var rk = Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).GetKey(TestRegistryKeyName, false))
        {
            Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(valueName));
        }

        // Should throw if RegistryKey is closed
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.DeleteValue(valueName);
        });
    }

    [Fact]
    public void DeleteValue_Test()
    {
        // [] Vanilla case, deleting a value
        Assert.Empty(TestRegistryKey.GetValueNames());
        TestRegistryKey.SetValue(TestRegistryKeyName, 5);
        Assert.Single(TestRegistryKey.GetValueNames());
        TestRegistryKey.DeleteValue(TestRegistryKeyName);
        Assert.Empty(TestRegistryKey.GetValueNames());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void DeleteValue_Default(string? defaultName)
    {
        // [] Vanilla case, deleting a value
        Assert.Empty(TestRegistryKey.GetValueNames());
        TestRegistryKey.SetValue(defaultName, 5);
        Assert.Single(TestRegistryKey.GetValueNames());
        TestRegistryKey.DeleteValue(defaultName);
        Assert.Empty(TestRegistryKey.GetValueNames());
    }

    [Fact]
    public void DeleteValue_Test04()
    {
        // [] Vanilla case , add a  bunch different objects and then Delete them
        var testCases = TestData.TestValueTypes.ToArray();
        foreach (var testCase in testCases)
        {
            TestRegistryKey.SetValue(testCase[0].ToString(), testCase[1]);
        }

        Assert.Equal(expected: testCases.Length, actual: TestRegistryKey.GetValueNames().Length);

        foreach (var testCase in testCases)
        {
            TestRegistryKey.DeleteValue(testCase[0].ToString());
        }

        Assert.Equal(expected: 0, actual: TestRegistryKey.GetValueNames().Length);
    }
}