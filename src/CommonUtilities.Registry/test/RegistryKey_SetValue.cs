using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

// Test Suite based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Win32.Registry/tests
public partial class RegistryTestsBase
{
    [Fact]
    public void SetValue_Test01()
    {
        // [] Passing in null should throw ArgumentNullException
        //UPDATE: This sets the default value. We should move this test to a newly defined reg key so as not to screw up the system
        const string expected = "This is a test";
        TestRegistryKey.SetValue(null, expected);
        Assert.Equal(expected, TestRegistryKey.GetValue(null));
    }

    [Fact]
    public void SetValue_NegativeTests()
    {
        // Should throw if passed value is null
        Assert.Throws<ArgumentNullException>(() => TestRegistryKey.SetValue("test", null!));
        
        // Should throw if RegistryKey closed
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.SetValue("TestValue", 42);
        });
    }

    public static IEnumerable<object[]> SetValueTestValueTypes => TestData.TestValueTypes;

    [Theory]
    [MemberData(nameof(SetValueTestValueTypes))]
    public void SetValue_WithValueTypes(string valueName, object testValue)
    {
        TestRegistryKey.SetValue(valueName, testValue);
        Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName)!.ToString());
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void SetValue_WithInt32()
    {
        const string testValueName = "Int32";
        const int expected = -5;

        TestRegistryKey.SetValue(testValueName, expected);
        Assert.Equal(expected, (int)TestRegistryKey.GetValue(testValueName)!);
        TestRegistryKey.DeleteValue(testValueName);
    }

    [Fact]
    public void SetValue_WithUInt64()
    {
        // This will be written as REG_SZ
        const string testValueName = "UInt64";
        const ulong expected = ulong.MaxValue;

        TestRegistryKey.SetValue(testValueName, expected);
        Assert.Equal(expected, Convert.ToUInt64(TestRegistryKey.GetValue(testValueName)));
        TestRegistryKey.DeleteValue(testValueName);
    }

    [Fact]
    public void SetValue_WithByteArray()
    {
        // This will be written as  REG_BINARY
        const string testValueName = "UBArr";
        byte[] expected = [1, 2, 3];

        TestRegistryKey.SetValue(testValueName, expected);
        Assert.Equal(expected, (byte[])TestRegistryKey.GetValue(testValueName)!);
        TestRegistryKey.DeleteValue(testValueName);
    }

    [Fact]
    public void SetValue_WithMultiString()
    {
        // This will be written as  REG_MULTI_SZ
        const string testValueName = "StringArr";
        string[] expected =
        [
            "This is a public",
                "broadcast intend to test",
                "lot of things. one of which"
        ];

        TestRegistryKey.SetValue(testValueName, expected);
        Assert.Equal(expected, (string[])TestRegistryKey.GetValue(testValueName)!);
        TestRegistryKey.DeleteValue(testValueName);
    }

    public static IEnumerable<object[]> SetValueTestEnvironment => TestData.TestEnvironment;

    [Theory]
    [MemberData(nameof(SetValueTestEnvironment))]
    public void SetValue_WithEnvironmentVariable(string valueName, string envVariableName, string expectedVariableValue)
    {
        // ExpandEnvironmentStrings is converting "C:\Program Files (Arm)" to "C:\Program Files (x86)".
        if (envVariableName == "ProgramFiles" && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            return; // see https://github.com/dotnet/runtime/issues/25778

        var value = "%" + envVariableName + "%";
        TestRegistryKey.SetValue(valueName, value);

        var result = (string)TestRegistryKey.GetValue(valueName)!;
        //we don't expand for the user, REG_SZ_EXPAND not supported
        Assert.Equal(expectedVariableValue, Environment.ExpandEnvironmentVariables(result));
        TestRegistryKey.DeleteValue(valueName);
    }

    [Fact]
    public void SetValue_WithEmptyString()
    {
        // Creating REG_SZ key with an empty string value does not add a null terminating char.
        const string testValueName = "test_122018";
        var expected = string.Empty;

        TestRegistryKey.SetValue(testValueName, expected);
        Assert.Equal(expected, (string)TestRegistryKey.GetValue(testValueName)!);
        TestRegistryKey.DeleteValue(testValueName);
    }

    [Fact]
    public void SetValue_OnDeletedKeyShouldThrow()
    {
        using var sub = TestRegistryKey.CreateSubKey("sub", writable: true);
        TestRegistryKey.DeleteKey("sub", true);
        Assert.Null(TestRegistryKey.OpenSubKey("sub"));
        Assert.Throws<IOException>(() => sub!.SetValue("name", 123));
    }
}