using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public class InMemoryRegistryKeyTest
{
    [Fact]
    public void GetAndSetDefaultValueTest()
    {
        var registry = new InMemoryRegistry();
        using var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        key.SetValue(null, "Default");
        key.GetValue(null!, out object value);
        Assert.Equal("Default", value);
        value = key.GetValue(null, "other Default")!;
        Assert.Equal("Default", value);
    }

    [Fact]
    public void TestKeyNotExists()
    {
        var registry = new InMemoryRegistry();
        using var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        var subKey = key.GetKey("Missing");
        Assert.Null(subKey);
    }

    [Fact]
    public void TestCan_Create_Deep_SubKey()
    {
        var registry = new InMemoryRegistry();
        using var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        var subKey = key.CreateSubKey(@"Deep\SubKey");
        Assert.NotNull(subKey);
        Assert.Equal("SubKey", subKey!.Name);
    }

    [Fact]
    public void TestDeleteValue()
    {
        var registry = new InMemoryRegistry();
        using var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        var subKey = key.CreateSubKey(@"Path");
        subKey!.SetValue("Test", true);

        subKey.GetValue("Test", out bool value);
        Assert.True(value);

        var deleted = subKey.DeleteValue("Test");
        Assert.True(deleted);
    }

    [Fact]
    public void TestDeleteSubValue()
    {
        var registry = new InMemoryRegistry();
        using var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        var subKey = key.CreateSubKey(@"Path\Sub");
        subKey!.SetValue("Test", true);
        
        var deleted = key.DeleteValue("Test", @"Path\Sub");
        Assert.True(deleted);
    }

    [Fact]
    public void TestDeleteKey()
    {
        var registry = new InMemoryRegistry();
        using var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        _ = key.CreateSubKey(@"Path\Sub");

        var deleted = key.DeleteKey("Path", false);
        Assert.False(deleted);

        deleted = key.DeleteKey("Path", true);
        Assert.True(deleted);

        Assert.Null(key.GetKey(@"Path\Sub"));
        Assert.Null(key.GetKey(@"Path"));
    }
}