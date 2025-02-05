﻿using System;
using System.Collections.Generic;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

// Test Suite based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Win32.Registry/tests
public partial class RegistryTestsBase
{
    [Fact]
    public void GetValue_NegativeTests()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.GetValue(null);
        });

        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.GetValue(null, TestData.DefaultValue);
        });
    }
    
    [Fact]
    public void GetValue_GetDefaultValueTest()
    {
        TestRegistryKey.SetValue(null, TestData.DefaultValue);
        Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(null));
        Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(string.Empty));
    }

    [Fact]
    public void GetValue_DisposedKeyDoesNotDeleteData()
    {
        var rk = TestRegistryKey.OpenSubKey(string.Empty, true)!;
        rk.SetValue(null, TestData.DefaultValue);
        rk.Dispose();
        Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(null));
    }

    [Fact]
    public void GetValue_RegistryKeyGetValueMultiStringDoesNotDiscardZeroLengthStrings()
    {
        const string valueName = "Test";
        string[] expected = ["", "Hello", "", "World", ""];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, (string[])TestRegistryKey.GetValue(valueName)!);
        TestRegistryKey.DeleteValue(valueName);
    }

    public static IEnumerable<object[]> GetValueTestValueTypes => TestData.TestValueTypes;

    [Theory]
    [MemberData(nameof(GetValueTestValueTypes))]
    public void GetValue_TestGetValueWithValueTypes(string valueName, object testValue)
    {
        TestRegistryKey.SetValue(valueName, testValue);
        Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName)!.ToString());
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValue_GetStringTest()
    {
        const string valueName = "Test";
        const string expected = "Here is a little test string";

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, TestRegistryKey.GetValue(valueName)!.ToString());
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValue_GetByteArrayTest()
    {
        const string valueName = "UBArr";
        byte[] expected = [1, 2, 3];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, (byte[])TestRegistryKey.GetValue(valueName)!);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValue_GetStringArrayTest()
    {
        const string valueName = "StringArr";
        string[] expected =
        [
            "This is a public",
                "broadcast intend to test",
                "lot of things. one of which"
        ];

        TestRegistryKey.SetValue(valueName, expected);
        Assert.Equal(expected, (string[])TestRegistryKey.GetValue(valueName)!);
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValue_Default_GetDefaultValueTest()
    {
        if (!TestRegistryKey.HasValue(null))
        {
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(null, TestData.DefaultValue));
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(string.Empty, TestData.DefaultValue));
        }

        TestRegistryKey.SetValue(null, TestData.DefaultValue);
        Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(null, null));
        Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(string.Empty, null));
    }

    [Fact]
    public void GetValue_Default_ShouldAcceptNullAsDefaultValue()
    {
        Assert.Null(TestRegistryKey.GetValue("tt", defaultValue: null));
    }

    [Theory]
    [MemberData(nameof(GetValueTestValueTypes))]
    public void GetValue_Default_TestGetValueWithValueTypes(string valueName, object testValue)
    {
        TestRegistryKey.SetValue(valueName, testValue);
        Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName, null)!.ToString());
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void GetValue_GetValueFromDeletedKey()
    {
        using var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        TestRegistryKey.DeleteKey(TestRegistryKeyName, true);

        // Assert does not throw
        rk!.GetValue("13");
    }
}