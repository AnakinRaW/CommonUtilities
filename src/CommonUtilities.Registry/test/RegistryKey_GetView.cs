using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

// Test Suite based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Win32.Registry/tests
public partial class RegistryTestsBase
{
    [Fact]
    public void GetView_NegativeTests()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            return TestRegistryKey.View;
        });
    }

    [Fact]
    public void GetView_Test()
    {
        Assert.Equal(RegistryView.Default, TestRegistryKey.View);
    }
}