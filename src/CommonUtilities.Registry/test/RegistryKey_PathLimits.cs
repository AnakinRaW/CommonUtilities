using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void CreateSubkey_MaxKeyLengthPerKeyPartExceeded()
    {
        // Should throw if key length above 255 characters
        const int maxValueNameLength = 255;
        var keyName = new string('a', maxValueNameLength) + "\\" + new string('b', maxValueNameLength + 1);

        if (HasPathLimits)
        {
            Assert.Throws<ArgumentException>(() => TestRegistryKey.CreateSubKey(keyName));
            Assert.Throws<ArgumentException>(() => TestRegistryKey.OpenSubKey(keyName));
        }
        else
        {
            Assert.NotNull(TestRegistryKey.CreateSubKey(keyName));
            Assert.NotNull(TestRegistryKey.OpenSubKey(keyName));
        }
    }

    [Fact]
    public void CreateSubkey_MaxKeyLengthNotReachedPerKeyPart()
    {
        const int maxValueNameLength = 255;
        var keyName = new string('a', maxValueNameLength) + "\\" + new string('b', maxValueNameLength);

        Assert.NotNull(TestRegistryKey.CreateSubKey(keyName));
        Assert.NotNull(TestRegistryKey.OpenSubKey(keyName));
    }

    [Fact]
    public void SetValue_MaxNameLength()
    {
        // Should throw if key length above 255 characters but prior to V4, the limit is 16383
        const int maxValueNameLength = 16383;
        var valueName = new string('a', maxValueNameLength + 1);

        if (HasPathLimits)
        {
            Assert.Throws<ArgumentException>(() => TestRegistryKey.SetValue(valueName, 5));
        }
        else
        {
            TestRegistryKey.SetValue(valueName, 5);
            Assert.True(TestRegistryKey.HasValue(valueName));
        }
    }
}