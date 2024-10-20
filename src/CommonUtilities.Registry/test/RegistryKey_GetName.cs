using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void GetName_NegativeTests()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            return TestRegistryKey.Name;
        });
    }

    [Fact]
    public void GetName_TestBaseKeyName()
    {
        Assert.Equal(CurrentUserKeyName, Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).Name);
    }

    [Fact]
    public void GetName_TestSubKeyName()
    {
        string expectedName = $"HKEY_CURRENT_USER\\{TestRegistryKeyName}";
        Assert.Equal(expectedName, TestRegistryKey.Name);
    }
}