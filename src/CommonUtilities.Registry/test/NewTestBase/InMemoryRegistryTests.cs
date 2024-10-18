using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public class InMemoryRegistryTests : RegistryTestsBase
{
    protected override IRegistry CreateRegistry()
    {
        var registry = new InMemoryRegistry();
        Assert.False(registry.IsCaseSensitive);
        return registry;
    }
}