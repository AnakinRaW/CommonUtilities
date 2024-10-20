using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void GetValueOrSetDefault_NegativeTests()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.GetValueOrSetDefault(null, TestData.DefaultValue, out _);
        });
    }

    [Fact]
    public void GetValueOrSetDefault_RegistryKeyGetValueMultiStringDoesNotDiscardZeroLengthStrings()
    {
        const string valueName = "Test";
        string[] expected = ["", "Hello", "", "World", ""];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValueOrSetDefault<string[]>(valueName, [""], out var defaultUsed));
        Assert.False(defaultUsed);
        TestRegistryKey.DeleteValue(valueName);
    }
    
    [Theory]
    [MemberData(nameof(GetValue_TestValueTypes))]
    public void GetValueOrSetDefault_TestGetValueWithValueTypes(string valueName, object testValue)
    {
        TestRegistryKey.SetValue(valueName, testValue);
        Assert.Equal(testValue.ToString(), TestRegistryKey.GetValueOrSetDefault<object>(valueName, TestData.DefaultValue, out var defaultUsed).ToString());
        Assert.False(defaultUsed);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValueOrSetDefault_GetStringTest()
    {
        const string valueName = "Test";
        const string expected = "Here is a little test string";

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValueOrSetDefault(valueName, TestData.DefaultValue, out var defaultUsed));
        Assert.False(defaultUsed);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValueOrSetDefault_GetByteArrayTest()
    {
        const string valueName = "UBArr";
        byte[] expected = [1, 2, 3];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValueOrSetDefault<byte[]>(valueName, [0, 0], out var defaultUsed));
        Assert.False(defaultUsed);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValueOrSetDefault_GetStringArrayTest()
    {
        const string valueName = "StringArr";
        string[] expected =
        [
            "This is a public",
                "broadcast intend to test",
                "lot of things. one of which"
        ];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValueOrSetDefault<string[]>(valueName, [""], out var defaultUsed));
        Assert.False(defaultUsed);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValueOrSetDefault_ShouldAcceptNullAsDefaultValue()
    {
        Assert.Null(TestRegistryKey.GetValueOrSetDefault<string>("tt", defaultValue: null, out var defaultUsed));
        Assert.True(defaultUsed);
    }

    [Fact]
    public void GetValueOrSetDefault_ShouldAcceptNullAsDefaultValue_Nullable()
    {
        Assert.Null(TestRegistryKey.GetValueOrSetDefault<int?>("tt", defaultValue: null, out var defaultUsed));
        Assert.True(defaultUsed);
    }

    [Fact]
    public void GetValueOrSetDefault_ConvertToEnum()
    {
        TestRegistryKey.SetValue("TestEnum", MyEnum.B);
        var value = TestRegistryKey.GetValueOrSetDefault("TestEnum", MyEnum.A, out var defaultUsed);
        Assert.Equal(MyEnum.B, value);
        Assert.False(defaultUsed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValueOrSetDefault_ConvertToBoolean(bool expectedValue)
    {
        TestRegistryKey.SetValue("flag", expectedValue);
        var value = TestRegistryKey.GetValueOrSetDefault("flag", false, out var defaultUsed);
        Assert.False(defaultUsed);
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void GetValueOrSetDefault()
    {
        var value = TestRegistryKey.GetValueOrSetDefault("value", 99, out var defaultUsed);
        Assert.Equal(99, value);
        Assert.True(defaultUsed);

        Assert.Equal(99, TestRegistryKey.GetValue("value"));

        value = TestRegistryKey.GetValueOrSetDefault("value", 1, out defaultUsed);
        Assert.Equal(99, value);
        Assert.False(defaultUsed);
    }

    [Fact]
    public void GetValueOrSetDefault_DefaultNullShallNotSet()
    {
        var value = TestRegistryKey.GetValueOrSetDefault<int?>("value", null, out var defaultUsed);
        Assert.Null(value);
        Assert.True(defaultUsed);
        Assert.False(TestRegistryKey.HasValue("value"));
    }
}