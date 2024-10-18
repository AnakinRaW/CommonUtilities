using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public partial class RegistryTestsBase
{
    public static readonly object[][] BaseKeyNameTestData =
    [
        [RegistryHive.CurrentUser, "HKEY_CURRENT_USER"],
        [RegistryHive.LocalMachine, "HKEY_LOCAL_MACHINE"],
        [RegistryHive.ClassesRoot, "HKEY_CLASSES_ROOT"],
    ];

    [Theory]
    [MemberData(nameof(BaseKeyNameTestData))]
    public void BaseKeyName_ExpectedName(RegistryHive hive, string expectedName)
    {
        var baseKey = Registry.OpenBaseKey(hive, RegistryView.Default);
        Assert.Equal(expectedName, baseKey.Name);
    }
}