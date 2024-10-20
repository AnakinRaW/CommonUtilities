using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public abstract class InMemoryRegistryTestsBase : RegistryTestsBase
{
    protected sealed override IRegistry CreateRegistry()
    {
        return new InMemoryRegistry(IsCaseSensitive);
    }
}

public class InMemoryRegistryTests_CaseInsensitive : InMemoryRegistryTestsBase
{
    public override bool IsCaseSensitive => false;
}

public class InMemoryRegistryTests_CaseISensitive : InMemoryRegistryTestsBase
{
    public override bool IsCaseSensitive => true;
}

