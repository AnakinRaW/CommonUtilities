using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

// Test Suite based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Win32.Registry/tests
public partial class RegistryTestsBase
{
    [Fact]
    public void HasPath_NegativeTests()
    {
        // Should throw if passed subkey name is null
        Assert.Throws<ArgumentNullException>(() => TestRegistryKey.HasPath(null!));

        // Should throw if RegistryKey closed
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.HasPath("");
        });
    }

    [Fact]
    public void HasPath_Test1()
    {
        Assert.True(TestRegistryKey.HasPath(string.Empty));
        Assert.False(TestRegistryKey.HasPath(TestRegistryKeyName));
        TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        Assert.True(TestRegistryKey.HasPath(TestRegistryKeyName));
        TestRegistryKey.DeleteKey(TestRegistryKeyName, true);
        Assert.False(TestRegistryKey.HasPath(TestRegistryKeyName));
        TestRegistryKey.DeleteKey(string.Empty, true);
        Assert.False(TestRegistryKey.HasPath(string.Empty));
    }
}