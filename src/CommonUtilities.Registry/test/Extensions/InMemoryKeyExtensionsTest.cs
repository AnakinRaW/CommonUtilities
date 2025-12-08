namespace AnakinRaW.CommonUtilities.Registry.Test.Extensions;

// ReSharper disable once UnusedMember.Global
public class InMemoryKeyExtensionsTest : RegistryKeyExtensionsTestBase
{
    protected override RegKeyTest CreateTestKey()
    {
        return new RegKeyTest(new InMemoryRegistry());
    }
}