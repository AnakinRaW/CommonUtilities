using System;
using System.IO;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    public abstract bool IsCaseSensitive { get; }

    [Fact]
    public void IsCaseSensitive_Test()
    {
        Assert.Equal(IsCaseSensitive, Registry.IsCaseSensitive);
        Assert.Equal(IsCaseSensitive, Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).IsCaseSensitive);
        Assert.Equal(IsCaseSensitive, TestRegistryKey.IsCaseSensitive);
        using var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        Assert.Equal(IsCaseSensitive, rk.IsCaseSensitive);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaseSensitive_GetValueFromDifferentKeys(bool useSeparator)
    {
        const int expectedValue = 11;
        const int defaultValue = 42;
        const string valueName = "value123";
        try
        {
            TestRegistryKey.SetValue(valueName, expectedValue);
            IRegistryKey? key = null;
            try
            {
                var keyName = MixUpperAndLowerCase(TestRegistryKeyName) + (useSeparator ? "\\" : "");

                key = Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).GetKey(keyName);

                if (IsCaseSensitive)
                {
                    Assert.Null(key);
                    Assert.Equal(defaultValue, TestRegistryKey.GetValue(MixUpperAndLowerCase(valueName), defaultValue));
                    Assert.False(TestRegistryKey.HasValue(MixUpperAndLowerCase(valueName)));
                }
                else
                {
                    Assert.NotNull(key);
                    Assert.Equal(expectedValue, key.GetValue(MixUpperAndLowerCase(valueName), defaultValue));
                    Assert.True(TestRegistryKey.HasValue(valueName));
                }
            }
            finally
            {
                key?.DeleteValue(valueName);
                Assert.True(TestRegistryKey.HasValue(valueName));
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaseSensitive_CreateSubKeyForDifferentKeys(bool useSeparator)
    {
        TestRegistryKey.CreateSubKey(TestRegistryKeyName);
        try
        {
            var keyName = MixUpperAndLowerCase(TestRegistryKeyName) + (useSeparator ? "\\" : "");

            var key = TestRegistryKey.CreateSubKey(keyName);
            Assert.NotNull(key);

            if (IsCaseSensitive)
            {
                Assert.Equal(2, TestRegistryKey.GetSubKeyNames().Length);
            }
            else
            {
                Assert.Single(TestRegistryKey.GetSubKeyNames());
            }

            TestRegistryKey.DeleteKey(keyName, false);

            if (IsCaseSensitive)
            {
                Assert.Single(TestRegistryKey.GetSubKeyNames());
                Assert.True(TestRegistryKey.HasPath(TestRegistryKeyName));
            }
            else
            {
                Assert.Empty(TestRegistryKey.GetSubKeyNames());
            }

        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    private static string MixUpperAndLowerCase(string str)
    {
        var builder = new System.Text.StringBuilder(str);

        for (var i = 0; i < builder.Length; ++i)
        {
            if (i % 2 == 0)
                builder[i] = char.ToLowerInvariant(builder[i]);
            else
                builder[i] = char.ToUpperInvariant(builder[i]);
        }

        return builder.ToString();
    }
}