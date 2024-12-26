#if Windows

using AnakinRaW.CommonUtilities.Registry.Windows;

namespace AnakinRaW.CommonUtilities.Registry.Test.Extensions;

public class WindowsKeyExtensionsTest : RegistryKeyExtensionsTestBase
{
    protected override RegKeyTest CreateTestKey()
    {
        return new RegKeyTest(new WindowsRegistry());
    }
}

#endif