using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void GetValue_Generic_NegativeTests()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.GetValue<int>(null);
            TestRegistryKey.GetValue<string>(null);
        });
    }

    [Fact]
    public void GetValue_Generic_GetDefaultValueTest()
    {
        TestRegistryKey.SetValue(null, TestData.DefaultValue);
        Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue<string>(null));
        Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue<string>(string.Empty));
    }

    [Fact]
    public void GetValue_Generic_RegistryKeyGetValueMultiStringDoesNotDiscardZeroLengthStrings()
    {
        const string valueName = "Test";
        string[] expected = ["", "Hello", "", "World", ""];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValue<string[]>(valueName));
        TestRegistryKey.DeleteValue(valueName);
    }
    
    [Theory]
    [MemberData(nameof(GetValue_TestValueTypes))]
    public void GetValue_Generic_TestGetValueWithValueTypes(string valueName, object testValue)
    {
        TestRegistryKey.SetValue(valueName, testValue);
        Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue<object>(valueName).ToString());
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValue_Generic_GetStringTest()
    {
        const string valueName = "Test";
        const string expected = "Here is a little test string";

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValue<string>(valueName));
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValue_Generic_GetByteArrayTest()
    {
        const string valueName = "UBArr";
        byte[] expected = [1, 2, 3];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValue<byte[]>(valueName));
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValue_Generic_GetStringArrayTest()
    {
        const string valueName = "StringArr";
        string[] expected =
        [
            "This is a public",
                "broadcast intend to test",
                "lot of things. one of which"
        ];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValue<string[]>(valueName));
        TestRegistryKey.DeleteValue(valueName);
    }  
    
    
    [Fact]
    public void GetValue_Generic_Enum()
    {
        TestRegistryKey.SetValue("TestEnum", MyEnum.B);
        var value = TestRegistryKey.GetValue<MyEnum>("TestEnum");
        Assert.Equal(MyEnum.B, value);
    }

    [Fact]
    public void GetValue_Generic_ConvertToEnum()
    {
        TestRegistryKey.SetValue("TestEnum", "b");
        var value = TestRegistryKey.GetValue<MyEnum>("TestEnum");
        Assert.Equal(MyEnum.B, value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValue_Generic_ConvertToBoolean(bool expectedValue)
    {
        TestRegistryKey.SetValue("flag", expectedValue);
        var value = TestRegistryKey.GetValue<bool>("flag");
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void GetValue_Generic_WithUInt64()
    {
        // This will be written as REG_SZ
        const string testValueName = "UInt64";
        const ulong expected = ulong.MaxValue;

        TestRegistryKey.SetValue(testValueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValue<ulong>(testValueName));
        TestRegistryKey.DeleteValue(testValueName);
    }

    [Fact]
    public void GetValue_Generic_CannotConvertType()
    {
        const string testValueName = "testFailedConversion";

        TestRegistryKey.SetValue(testValueName, "abc");

        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<ulong>(testValueName));
        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<int>(testValueName));
        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<byte[]>(testValueName));
        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<MyEnum>(testValueName));
        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<bool>(testValueName));
    }
}