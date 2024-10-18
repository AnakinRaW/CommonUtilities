using System;
using System.Text;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

//public abstract class RegistryTestBase : IDisposable
//{
//    protected virtual string? SubKeyPath => null;

//    protected abstract IRegistry CreateRegistry();

//    protected virtual IRegistryKey CreateRegistryWithKey()
//    {
//        var registry = CreateRegistry();
//        var baseKey = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

//        if (!string.IsNullOrEmpty(SubKeyPath))
//        {
//            baseKey.DeleteKey(SubKeyPath, true);
//            return baseKey.CreateSubKey(SubKeyPath)!;
//        }

//        return baseKey;

//    }

//    public virtual void Dispose()
//    {
//    }

//    [Fact]
//    public void Test_CtorSetPros()
//    {
//        using var key = CreateRegistryWithKey();
//        Assert.Equal(RegistryView.Registry64, key.View);
//        Assert.Empty(key.GetSubKeyNames()!);
//        Assert.Empty(key.GetSubKeyNames("")!);
//        Assert.Null(key.GetSubKeyNames("notExists"));
//    }

//    [Fact]
//    public void GetValueAndSetValue()
//    {
//        using var key = CreateRegistryWithKey();

//        Assert.Null(key.GetValue("value"));
//        Assert.Null(key.GetValue("value", null));
//        Assert.False(key.GetValue("value", out int? value));
//        Assert.Null(value);

//        key.SetValue("value", 123);

//        Assert.Equal(123, key.GetValue("value"));
//        Assert.Equal(123, key.GetValue("value", null));
//        Assert.True(key.GetValue("value", out value));
//        Assert.Equal(123, value);
//    }

//    [Fact]
//    public void GetValueAndSetValue_WithSelfSubKey()
//    {
//        using var key = CreateRegistryWithKey();

//        Assert.False(key.GetValue("value", string.Empty, out int? value));
//        Assert.Null(value);

//        Assert.True(key.SetValue("value", string.Empty, 123));
//        Assert.True(key.GetValue("value", string.Empty, out value));
//        Assert.Equal(123, value);
//    }

//    [Fact]
//    public void GetValueAndSetValue_WithSubKey()
//    {
//        using var key = CreateRegistryWithKey();

//        Assert.False(key.GetValue("value", "sub", out int? value));
//        Assert.Null(value);

//        // Sub key does not exist
//        Assert.False(key.SetValue("value", "sub", 123));
//        Assert.False(key.GetValue("value", "sub", out value));
//        Assert.Null(value);

//        using var sub = key.CreateSubKey("sub");

//        Assert.True(key.SetValue("value", "sub", 123));
//        Assert.True(key.GetValue("value", "sub", out value));
//        Assert.Equal(123, value);
//    }

//    [Fact]
//    public void GetValueOrDefault()
//    {
//        using var key = CreateRegistryWithKey();
//        Assert.False(key.GetValueOrDefault("value", out var value, 99));
//        Assert.Equal(99, value);

//        key.SetValue("value", 1);
//        Assert.True(key.GetValueOrDefault("value", out value, 99));
//        Assert.Equal(1, value);
//    }

//    [Fact]
//    public void GetValueOrSetDefault()
//    {
//        using var key = CreateRegistryWithKey();
//        var value = key.GetValueOrSetDefault("value", 99, out var defaultUsed);
//        Assert.Equal(99, value);
//        Assert.True(defaultUsed);

//        Assert.True(key.GetValue("value", out value));
//        Assert.Equal(99, value);

//        value = key.GetValueOrSetDefault("value", 1, out defaultUsed);
//        Assert.Equal(99, value);
//        Assert.False(defaultUsed);
//    }

//    [Fact]
//    public void GetValueOrSetDefault_DefaultNullShallNotSet()
//    {
//        using var key = CreateRegistryWithKey();
//        var value = key.GetValueOrSetDefault<int?>("value", null, out var defaultUsed);
//        Assert.Null(value);
//        Assert.True(defaultUsed);
//        Assert.False(key.HasValue("value"));
//    }

//    [Fact]
//    public void GetValueOrSetDefault_WithSubKey()
//    {
//        using var key = CreateRegistryWithKey();
//        var value = key.GetValueOrSetDefault("value", "sub", 99, out var defaultUsed);
//        Assert.Equal(99, value);
//        Assert.True(defaultUsed);
//        // GetValueOrSetDefault does not create the subkey.
//        Assert.False(key.HasPath("sub"));

//        using var sub = key.CreateSubKey("sub");
//        Assert.False(sub!.HasValue("value"));
//        value = key.GetValueOrSetDefault("value", "sub", 99, out defaultUsed);
//        Assert.Equal(99, value);
//        Assert.True(defaultUsed);
//        Assert.True(sub!.HasValue("value"));

//        value = key.GetValueOrSetDefault("value", "sub", 99, out defaultUsed);
//        Assert.Equal(99, value);
//        Assert.False(defaultUsed);

//    }

//    [Fact]
//    public void Test_SetValue_GetValue_WithDefaults()
//    {
//        using var key = CreateRegistryWithKey();
//        key.SetValue(null, "Default");
//        Assert.True(key.GetValue(null!, out object? value));
//        Assert.Equal("Default", value);
//        value = key.GetValue(null, "other Default")!;
//        Assert.Equal("Default", value);
//    }

//    [Fact]
//    public void Test_GetKey_KeyNotExists()
//    {
//        using var key = CreateRegistryWithKey();
//        var subKey = key.GetKey("Missing");
//        Assert.Null(subKey);
//    }

//    [Fact]
//    public void Test_CreateSubKey_CanCreateDeepSubKey()
//    {
//        using var key = CreateRegistryWithKey();

//        // This value should not appear in the GetSubKeyNames array
//        key.SetValue("distract", 123);

//        Assert.False(key.HasPath("Deep\\SubKey"));

//        var subKey = key.CreateSubKey(@"Deep\SubKey");
//        Assert.Equal(["Deep"], key.GetSubKeyNames()!);
//        Assert.Equal(["Deep"], key.GetSubKeyNames("")!);
//        Assert.Equal(["SubKey"], key.GetSubKeyNames("Deep")!);
//        Assert.Equal(["SubKey"], key.GetKey("Deep")!.GetSubKeyNames()!);

//        Assert.True(key.HasPath("Deep\\SubKey"));
//        Assert.NotNull(subKey);

//        var expectedPath = new StringBuilder("HKEY_CURRENT_USER\\");
//        if (!string.IsNullOrEmpty(SubKeyPath))
//        {
//            expectedPath.Append(SubKeyPath);
//            expectedPath.Append('\\');
//        }

//        expectedPath.Append("Deep\\SubKey");

//        Assert.Equal(expectedPath.ToString(), subKey!.Name);

//        Assert.NotNull(key.GetKey("Deep\\SubKey"));
//    }

//    [Fact]
//    public void Test_DeleteValue()
//    {
//        using var key = CreateRegistryWithKey();
//        var subKey = key.CreateSubKey("Path");
//        subKey!.SetValue("Test", true);

//        subKey.GetValue("Test", out bool value);
//        Assert.True(value);

//        var deleted = subKey.DeleteValue("Test");
//        Assert.True(deleted);
//    }

//    [Fact]
//    public void Test_DeleteValue_DeleteSubValue()
//    {
//        using var key = CreateRegistryWithKey();
//        var subKey = key.CreateSubKey(@"Path\Sub");
//        subKey!.SetValue("Test", true);

//        var deleted = key.DeleteValue("Test", @"Path\Sub");
//        Assert.True(deleted);
//    }

//    [Fact]
//    public void Test_DeleteKey()
//    {
//        using var key = CreateRegistryWithKey();
//        _ = key.CreateSubKey(@"Path\Sub");

//        Assert.True(key.DeleteKey("DoesNotExist", true));
//        ;
//        Assert.False(key.DeleteKey("Path", false));
//        Assert.True(key.DeleteKey("Path", true));

//        Assert.Null(key.GetKey(@"Path\Sub"));
//        Assert.Null(key.GetKey("Path"));
//    }

//    [Fact]
//    public void SelfDelete()
//    {
//        SelfDeleteCore(key => key.DeleteKey());
//    }

//    [Fact]
//    public void SelfDelete_Overload()
//    {
//        SelfDeleteCore(key => key.DeleteKey("", true));
//    }


//    private void SelfDeleteCore(Func<IRegistryKey, bool> deleteAction)
//    {
//        using var key = CreateRegistryWithKey();
//        var sub = key.CreateSubKey("Sub");
//        sub!.CreateSubKey("SubSub");
//        Assert.True(deleteAction(sub));
//        Assert.False(key.HasPath("Sub"));
//    }

//    [Fact]
//    public void Delete_NonExistingKey()
//    {
//        using var key = CreateRegistryWithKey();
//        Assert.True(key.DeleteValue("notFound"));
//    }

//    // TODO
//    [Fact]
//    public void TestDataTypes_NonGenerics()
//    {
//        var key = CreateRegistryWithKey();

//        key.SetValue("TestEnum", 1);
//        key.GetValue("TestEnum", out int oi);
//        var i = Assert.IsType<int>(oi);
//        Assert.Equal(1, i);

//        key.SetValue("TestEnum", 1ul);
//        key.GetValue("TestEnum", out ulong oul);
//        var ul = Assert.IsType<ulong>(oul);
//        Assert.Equal(1ul, ul);

//        key.SetValue("TestBool", true);
//        key.GetValue("TestBool", out bool ob);
//        var b = Assert.IsType<bool>(ob);
//        Assert.True(b);

//        key.SetValue("TestEnum", MyEnum.B);
//        key.GetValue("TestEnum", out MyEnum oe);
//        var e = Assert.IsType<MyEnum>(oe);
//        Assert.Equal(MyEnum.B, e);
//    }

//    [Fact]
//    public void TestDataTypes_Generics()
//    {
//        var key = CreateRegistryWithKey();

//        key.SetValue("TestEnum", 1);
//        key.GetValue("TestEnum", out int oi);
//        var i = Assert.IsType<int>(oi);
//        Assert.Equal(1, i);

//        key.SetValue("TestEnum", 1ul);
//        key.GetValue("TestEnum", out ulong oul);
//        var ul = Assert.IsType<ulong>(oul);
//        Assert.Equal(1ul, ul);

//        key.SetValue("TestBool", true);
//        key.GetValue("TestBool", out bool ob);
//        var b = Assert.IsType<bool>(ob);
//        Assert.True(b);

//        key.SetValue("TestEnum", MyEnum.B);
//        key.GetValue("TestEnum", out MyEnum oe);
//        var e = Assert.IsType<MyEnum>(oe);
//        Assert.Equal(MyEnum.B, e);
//    }

//    [Fact]
//    public void SetValue_Default()
//    {
//        using var key = CreateRegistryWithKey();
//        Assert.False(key.HasValue(null));
//        Assert.False(key.HasValue(string.Empty));
//        Assert.False(key.GetValueOrDefault("", out int value, 0));
//        Assert.False(key.GetValueOrDefault(null!, out value, 0));

//        key.SetValue("", 1);
//        Assert.True(key.GetValue(null, out value));
//        Assert.True(key.HasValue(string.Empty));
//        Assert.Equal(1, value);
//        Assert.True(key.GetValueOrDefault(string.Empty, out value, 0));
//        Assert.Equal(1, value);
//        Assert.True(key.GetValueOrDefault(null!, out value, 0));
//        Assert.Equal(1, value);

//        key.SetValue(null, 2);
//        Assert.True(key.GetValue(string.Empty, out value));
//        Assert.True(key.HasValue(null));
//        Assert.Equal(2, value);
//        Assert.True(key.GetValueOrDefault(string.Empty, out value, 0));
//        Assert.Equal(2, value);
//        Assert.True(key.GetValueOrDefault(null!, out value, 0));
//        Assert.Equal(2, value);
//    }
    
//    [Fact]
//    public void HasPath_EmptyString()
//    {
//        using var key = CreateRegistryWithKey();
//        Assert.True(key.HasPath(string.Empty));
//    }

//    [Fact]
//    public void GetSetValue_CaseInsensitiveKey()
//    {
//        using var key = CreateRegistryWithKey();
//        Assert.False(key.IsCaseSensitive);
//        key.SetValue("value", 1);
//        Assert.True(key.GetValue("VALUE", out int value));
//        Assert.True(key.HasValue("VALue"));
//        Assert.Equal(1, value);
//        Assert.True(key.DeleteValue("vALue"));
//        Assert.False(key.HasValue("value"));
//    }

//    [Fact]
//    public void Path_CaseInsensitiveKey()
//    {
//        using var key = CreateRegistryWithKey();
//        Assert.False(key.IsCaseSensitive);
//        var sub = key.CreateSubKey("path");
//        Assert.False(sub.IsCaseSensitive);
//        Assert.True(key.HasPath("PATH"));
//        Assert.True(key.DeleteKey("paTH", true));
//        Assert.False(key.HasPath("path"));
//    }

//    [Fact]
//    public void Test_InvalidNullValues()
//    {
//        using var key = CreateRegistryWithKey();
//        Assert.Throws<ArgumentNullException>(() => key.CreateSubKey(null!));
//        Assert.Throws<ArgumentNullException>(() => key.GetSubKeyNames(null!));
//        Assert.Throws<ArgumentNullException>(() => key.GetKey(null!));
//        Assert.Throws<ArgumentNullException>(() => key.DeleteKey(null!, false));
//        Assert.Throws<ArgumentNullException>(() => key.DeleteKey(null!, true));
//        Assert.Throws<ArgumentNullException>(() => key.DeleteValue(null!));
//        Assert.Throws<ArgumentNullException>(() => key.SetValue("value", null!, 1));
//        Assert.Throws<ArgumentNullException>(() => key.SetValue("value", "sub", null!));
//        Assert.Throws<ArgumentNullException>(() => key.SetValue("value", null!));
//        Assert.Throws<ArgumentNullException>(() => key.GetValue<int?>("value", null!, out _));
//        Assert.Throws<ArgumentNullException>(() => key.GetValueOrSetDefault<int?>("value", null!, null, out _));
//        Assert.Throws<ArgumentNullException>(() => key.GetValueOrDefault<int?>("value", null!, out _, null));
//        Assert.Throws<ArgumentNullException>(() => key.HasPath(null!));
//    }

//    [Fact]
//    public void Test_CreateSubKey_ReturnsSelfForEmptyString()
//    {
//        using var key = CreateRegistryWithKey();
//        var other = key.CreateSubKey(string.Empty);
//        Assert.Equal(key.Name.TrimEnd('\\'), other!.Name.TrimEnd('\\'));
//    }

//    [Fact]
//    public void Test_GetKey_ReturnsSelfForEmptyString()
//    {
//        using var key = CreateRegistryWithKey();
//        var other = key.GetKey(string.Empty);
//        Assert.Equal(key.Name.TrimEnd('\\'), other!.Name.TrimEnd('\\'));
//    }

//    private enum MyEnum
//    {
//        A,
//        B
//    }
//}