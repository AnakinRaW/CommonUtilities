using System;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void SetValue_InvalidDataTypes_WindowsCompatibility()
    {
        if (HasTypeLimits)
        {
            // Should throw if passed value is array with uninitialized elements
            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.SetValue("StringArr", value: new string[1]));

            // Should throw because only String[] (REG_MULTI_SZ) and byte[] (REG_BINARY) are supported.
            // RegistryKey.SetValue does not support arrays of type UInt32[].
            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.SetValue("IntArray", value: new[] { 1, 2, 3 }));
        }
        else
        {
            TestRegistryKey.SetValue("StringArr", value: new string[1]);
            Assert.Equal([null!], TestRegistryKey.GetValue<string[]>("StringArr")!);
            TestRegistryKey.SetValue("IntArray", value: new[] { 1, 2, 3 });
            Assert.Equal([1, 2, 3], TestRegistryKey.GetValue<int[]>("IntArray")!);
        }
    }

    [Fact]
    public void SetValue_EmptyStringArrayIsOK()
    {
        TestRegistryKey.SetValue("StringArr", value: Array.Empty<string>());
        Assert.Equal([], TestRegistryKey.GetValue<string[]>("StringArr")!);
    }
}