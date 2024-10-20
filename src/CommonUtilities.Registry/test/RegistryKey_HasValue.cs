using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

// Test Suite based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Win32.Registry/tests
public partial class RegistryTestsBase
{
    [Fact]
    public void HasValue_NegativeTests()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.HasValue(null);
        });

        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.HasValue("name");
        });
    }

    [Fact]
    public void HasValue_DefaultValueTest()
    {
        Assert.False(TestRegistryKey.HasValue(null));
        Assert.False(TestRegistryKey.HasValue(string.Empty));
        TestRegistryKey.SetValue(null, TestData.DefaultValue);
        Assert.True(TestRegistryKey.HasValue(null));
        Assert.True(TestRegistryKey.HasValue(string.Empty));
    }

    [Fact]
    public void HasValue_NamedValueTest()
    {
        Assert.False(TestRegistryKey.HasValue("name"));
        TestRegistryKey.SetValue("name", TestData.DefaultValue);
        Assert.True(TestRegistryKey.HasValue("name"));
    }
}