#if Windows

using System.Runtime.Versioning;
using AnakinRaW.CommonUtilities.Registry.Windows;

namespace AnakinRaW.CommonUtilities.Registry.Test.Extensions;

// ReSharper disable once UnusedMember.Global
[SupportedOSPlatform("windows")]
public class WindowsKeyExtensionsTest : RegistryKeyExtensionsTestBase
{
    protected override RegKeyTest CreateTestKey()
    {
        return new RegKeyTest(new WindowsRegistry());
    }
}

#endif