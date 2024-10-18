using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

//public class InMemoryRegistryKeyTest : RegistryTestBase
//{
//    private static IRegistry CreateRegistry(bool caseSensitive)
//    {
//        var registry = new InMemoryRegistry(caseSensitive);
//        Assert.Equal(caseSensitive, registry.IsCaseSensitive);
//        return registry;
//    }

//    protected override IRegistry CreateRegistry()
//    {
//        return CreateRegistry(false);
//    }

//    private static IRegistryKey CreateRegistryWithKey(bool caseSensitive)
//    {
//        var registry = CreateRegistry(caseSensitive);
//        return registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
//    }

//    protected override IRegistryKey CreateRegistryWithKey()
//    {
//        return CreateRegistryWithKey(false);
//    }

//    [Fact]
//    public void SubName()
//    {
//        var key = (InMemoryRegistryKey) CreateRegistryWithKey();
//        Assert.Equal("HKEY_CURRENT_USER", key.Name);
//        Assert.Equal("HKEY_CURRENT_USER", key.SubName);

//        var subKey = (InMemoryRegistryKey)key.CreateSubKey("test\\sub\\PATH");
//        Assert.Equal("HKEY_CURRENT_USER\\test\\sub\\PATH", subKey!.Name);
//        Assert.Equal("PATH", subKey!.SubName);
//    }

//    [Fact]
//    public void GetSetValue_CaseSensitiveKey()
//    {
//        using var key = CreateRegistryWithKey(true);
//        Assert.True(key.IsCaseSensitive);

//        key.SetValue("value", 1);
//        Assert.False(key.GetValue("VALUE", out int? value));
//        Assert.False(key.HasValue("VALue"));
//        Assert.Null(value);
//        Assert.True(key.DeleteValue("vALue"));
//        Assert.True(key.HasValue("value"));
//    }

//    [Fact]
//    public void Path_CaseSensitiveKey()
//    {
//        using var key = CreateRegistryWithKey(true);
//        Assert.True(key.IsCaseSensitive);

//        using var sub = key.CreateSubKey("path");
//        Assert.True(sub!.IsCaseSensitive);
//        Assert.False(key.HasPath("PATH"));
//        Assert.True(key.DeleteKey("paTH", true));
//        Assert.True(key.HasPath("path"));
//    }
//}