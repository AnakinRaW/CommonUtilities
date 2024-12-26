namespace AnakinRaW.CommonUtilities.Registry.Test.Extensions;

public class InMemoryKeyExtensionsTest : RegistryKeyExtensionsTestBase
{
    protected override RegKeyTest CreateTestKey()
    {
        return new RegKeyTest(new InMemoryRegistry());
    }
}