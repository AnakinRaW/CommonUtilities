using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    [Fact]
    public void GetSubKeyNames_ShouldThrowIfDisposed()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.GetSubKeyNames();
        });
    }

    [Fact]
    public void GetSubKeyNames_ShouldThrowIfRegistryKeyDeleted()
    {
        Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).DeleteKey(TestRegistryKeyName, true);
        Assert.Throws<IOException>(() => TestRegistryKey.GetSubKeyNames());
    }

    [Fact]
    public void GetSubKeyNames_Test()
    {
        // [] Creating new SubKeys and get the names
        var expectedSubKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x).ToArray();
        foreach (var subKeyName in expectedSubKeyNames) 
            TestRegistryKey.CreateSubKey(subKeyName)!.Dispose();

        Assert.Equal(expectedSubKeyNames, TestRegistryKey.GetSubKeyNames());
    }

    [Fact]
    public void GetSubKeyNames_Test2()
    {
        // [] Check that zero length array is returned for no subkeys
        Assert.Empty(TestRegistryKey.GetSubKeyNames());
    }
}