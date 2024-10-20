namespace AnakinRaW.CommonUtilities.Registry.Test;

public abstract class InMemoryRegistryTestsBase : RegistryTestsBase
{
    protected sealed override IRegistry CreateRegistry()
    {
        var flags = InMemoryRegistryCreationFlags.Default;

        if (IsCaseSensitive)
            flags |= InMemoryRegistryCreationFlags.CaseSensitive;
        if (HasPathLimits)
            flags |= InMemoryRegistryCreationFlags.UseWindowsLengthLimits;
        if (HasPathLimits)
            flags |= InMemoryRegistryCreationFlags.OnlyUseWindowsDataTypes;
        return new InMemoryRegistry(flags);
    }
}

public class InMemoryRegistryTests_Default : InMemoryRegistryTestsBase
{
    public override bool IsCaseSensitive => false;
    public override bool HasPathLimits => false;
    public override bool HasTypeLimits => false;
}

public class InMemoryRegistryTests_LikeWindows : InMemoryRegistryTestsBase
{
    public override bool IsCaseSensitive => false;
    public override bool HasPathLimits => true;
    public override bool HasTypeLimits => true;
}

public class InMemoryRegistryTests_CaseISensitive : InMemoryRegistryTestsBase
{
    public override bool IsCaseSensitive => true;
    public override bool HasPathLimits => false;
    public override bool HasTypeLimits => false;
}

