using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void ToString_NegativeTests()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            return TestRegistryKey.ToString();
        });
    }

    [Fact]
    public void ToString_TestBaseKey()
    {
        Assert.Equal(CurrentUserKeyName, Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).ToString());
    }

    [Fact]
    public void ToString_TestIsName()
    {
        var expectedName = $"HKEY_CURRENT_USER\\{TestRegistryKeyName}";
        Assert.Equal(expectedName, TestRegistryKey.ToString());
    }
}