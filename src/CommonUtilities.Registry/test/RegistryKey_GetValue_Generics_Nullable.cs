using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

// Test Suite based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Win32.Registry/tests
public partial class RegistryTestsBase
{
    [Fact]
    public void GetValue_Generic_NegativeTests_Nullable()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.GetValue<int?>(null);
            TestRegistryKey.GetValue<uint?>(null);
            TestRegistryKey.GetValue<TestData.MyEnum?>(null);
        });
    }
    
    [Fact]
    public void GetValue_Generic_Enum_Nullable()
    {
        Assert.Null(TestRegistryKey.GetValue<TestData.MyEnum?>("TestEnum"));
        TestRegistryKey.SetValue("TestEnum", TestData.MyEnum.B);
        var value = TestRegistryKey.GetValue<TestData.MyEnum?>("TestEnum");
        Assert.Equal(TestData.MyEnum.B, value);
    }

    [Fact]
    public void GetValue_Generic_ConvertToEnum_Nullable()
    {
        Assert.Null(TestRegistryKey.GetValue<TestData.MyEnum?>("TestEnum"));
        TestRegistryKey.SetValue("TestEnum", "b");
        var value = TestRegistryKey.GetValue<TestData.MyEnum?>("TestEnum");
        Assert.Equal(TestData.MyEnum.B, value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValue_Generic_ConvertToBoolean_Nullable(bool expectedValue)
    {
        Assert.Null(TestRegistryKey.GetValue<bool?>("flag"));
        TestRegistryKey.SetValue("flag", expectedValue);
        var value = TestRegistryKey.GetValue<bool?>("flag");
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void GetValue_Generic_WithUInt64_Nullable()
    {
        // This will be written as REG_SZ
        const string testValueName = "UInt64";
        const ulong expected = ulong.MaxValue;

        Assert.Null(TestRegistryKey.GetValue<ulong?>(testValueName));

        TestRegistryKey.SetValue(testValueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValue<ulong?>(testValueName));
        TestRegistryKey.DeleteValue(testValueName);
    }

    [Fact]
    public void GetValue_Generic_CannotConvertType_Nullable()
    {
        const string testValueName = "testFailedConversion";

        TestRegistryKey.SetValue(testValueName, "abc");

        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<ulong?>(testValueName));
        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<int?>(testValueName));
        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<byte[]?>(testValueName));
        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<TestData.MyEnum?>(testValueName));
        Assert.Throws<ArgumentException>(() => TestRegistryKey.GetValue<bool?>(testValueName));
    }
}