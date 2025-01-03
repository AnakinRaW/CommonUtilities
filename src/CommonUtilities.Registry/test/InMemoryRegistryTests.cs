﻿using Xunit;

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
        var registry = flags == InMemoryRegistryCreationFlags.Default ? new InMemoryRegistry() : new InMemoryRegistry(flags);
        Assert.Equal(flags, registry.Flags);
        return registry;
    }
}

// ReSharper disable once UnusedMember.Global
// ReSharper disable once InconsistentNaming
public class InMemoryRegistryTests_Default : InMemoryRegistryTestsBase
{
    public override bool IsCaseSensitive => false;
    public override bool HasPathLimits => false;
    public override bool HasTypeLimits => false;
}

// ReSharper disable once UnusedMember.Global
// ReSharper disable once InconsistentNaming
public class InMemoryRegistryTests_LikeWindows : InMemoryRegistryTestsBase
{
    public override bool IsCaseSensitive => false;
    public override bool HasPathLimits => true;
    public override bool HasTypeLimits => true;
}

// ReSharper disable once UnusedMember.Global
// ReSharper disable once InconsistentNaming
public class InMemoryRegistryTests_CaseSensitive : InMemoryRegistryTestsBase
{
    public override bool IsCaseSensitive => true;
    public override bool HasPathLimits => false;
    public override bool HasTypeLimits => false;
}

