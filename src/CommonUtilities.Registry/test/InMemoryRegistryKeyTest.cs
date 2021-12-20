using Sklavenwalker.CommonUtilities.Registry;
using Xunit;

namespace CommonUtilities.Registry.Test;

public class InMemoryRegistryKeyTest
{
    [Fact]
    public void GetAndSetDefaultValueTest()
    {
        var registry = new InMemoryRegistry();
        using var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        key.SetValue(null, "Default");
        key.GetValue(null!, out object? value);
        Assert.Equal("Default", value);
        value = key.GetValue(null, "other Default")!;
        Assert.Equal("Default", value);
    }

    [Fact]
    public void KeyNotExists()
    {
        var registry = new InMemoryRegistry();
        using var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        var subKey = key.GetKey("Missing");
        Assert.Null(subKey);
    }

    [Fact]
    public void Can_Create_Deep_SubKey()
    {
        var registry = new InMemoryRegistry();
        using var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        var subKey = key.CreateSubKey(@"Deep\SubKey");
        Assert.NotNull(subKey);
        Assert.Equal("SubKey", subKey!.Name);
    }
}