using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void GetValueOrDefault_NegativeTests()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.GetValueOrDefault(null, TestData.DefaultValue, out _);
        });
    }

    [Fact]
    public void GetValueOrDefault_RegistryKeyGetValueMultiStringDoesNotDiscardZeroLengthStrings()
    {
        const string valueName = "Test";
        string[] expected = ["", "Hello", "", "World", ""];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValueOrDefault<string[]>(valueName, [""], out var exists));
        Assert.True(exists);
        TestRegistryKey.DeleteValue(valueName);
    }
    
    [Theory]
    [MemberData(nameof(GetValue_TestValueTypes))]
    public void GetValueOrDefault_TestGetValueWithValueTypes(string valueName, object testValue)
    {
        TestRegistryKey.SetValue(valueName, testValue);
        Assert.Equal(testValue.ToString(), TestRegistryKey.GetValueOrDefault<object>(valueName, TestData.DefaultValue, out var exists).ToString());
        Assert.True(exists);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValueOrDefault_GetStringTest()
    {
        const string valueName = "Test";
        const string expected = "Here is a little test string";

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValueOrDefault(valueName, TestData.DefaultValue, out var exists));
        Assert.True(exists);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValueOrDefault_GetByteArrayTest()
    {
        const string valueName = "UBArr";
        byte[] expected = [1, 2, 3];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValueOrDefault<byte[]>(valueName, [0, 0], out var exists));
        Assert.True(exists);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValueOrDefault_GetStringArrayTest()
    {
        const string valueName = "StringArr";
        string[] expected =
        [
            "This is a public",
                "broadcast intend to test",
                "lot of things. one of which"
        ];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValueOrDefault<string[]>(valueName, [""], out var exists));
        Assert.True(exists);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValueOrDefault_GetDefaultValueTest()
    {
        bool exists;
        if (!TestRegistryKey.HasValue(null))
        {
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValueOrDefault(null, TestData.DefaultValue, out exists));
            Assert.False(exists);
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValueOrDefault(string.Empty, TestData.DefaultValue, out exists));
            Assert.False(exists);
        }

        TestRegistryKey.SetValue(null, TestData.DefaultValue);
        Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValueOrDefault<string>(null, null, out exists));
        Assert.True(exists);
        Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValueOrDefault<string>(string.Empty, null, out exists));
        Assert.True(exists);
    }

    [Fact]
    public void GetValueOrDefault_ShouldAcceptNullAsDefaultValue()
    {
        Assert.Null(TestRegistryKey.GetValueOrDefault<string>("tt", defaultValue: null, out var exists));
        Assert.False(exists);
    }

    [Fact]
    public void GetValueOrDefault_ShouldAcceptNullAsDefaultValue_Nullable()
    {
        Assert.Null(TestRegistryKey.GetValueOrDefault<int?>("tt", defaultValue: null, out var exists));
        Assert.False(exists);
    }

    [Fact]
    public void GetValueOrDefault_ConvertToEnum()
    {
        TestRegistryKey.SetValue("TestEnum", MyEnum.B);
        var value = TestRegistryKey.GetValueOrDefault<MyEnum>("TestEnum", MyEnum.A, out var exists);
        Assert.Equal(MyEnum.B, value);
        Assert.True(exists);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValueOrDefault_ConvertToBoolean(bool expectedValue)
    {
        TestRegistryKey.SetValue("flag", expectedValue);
        var value = TestRegistryKey.GetValueOrDefault("flag", false, out var exists);
        Assert.True(exists);
        Assert.Equal(expectedValue, value);
    }
}