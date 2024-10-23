using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public abstract class InMemoryRegistryTestsBase : RegistryTestsBase
{
    private InMemoryRegistryCreationFlags CreateFlags()
    {
        var flags = InMemoryRegistryCreationFlags.Default;

        if (IsCaseSensitive)
            flags |= InMemoryRegistryCreationFlags.CaseSensitive;
        if (HasPathLimits)
            flags |= InMemoryRegistryCreationFlags.UseWindowsLengthLimits;
        if (HasPathLimits)
            flags |= InMemoryRegistryCreationFlags.OnlyUseWindowsDataTypes;
        return flags;
    }

    protected sealed override IRegistry CreateRegistry()
    {
        var flags = CreateFlags();
        var registry = flags == InMemoryRegistryCreationFlags.Default ? new InMemoryRegistry() : new InMemoryRegistry(flags);
        Assert.Equal(flags, registry.Flags);
        return registry;
    }

    [Fact]
    public void CtorThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InMemoryRegistryKeyData(RegistryView.Default, null!, null, CreateFlags(), true));
        Assert.Throws<ArgumentException>(() =>
            new InMemoryRegistryKeyData(RegistryView.Default, "", null, CreateFlags(), true));
        Assert.Throws<InvalidOperationException>(() => 
            new InMemoryRegistryKeyData(RegistryView.Default, "name", null, CreateFlags(), false));
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
public class InMemoryRegistryTests_CaseISensitive : InMemoryRegistryTestsBase
{
    public override bool IsCaseSensitive => true;
    public override bool HasPathLimits => false;
    public override bool HasTypeLimits => false;
}

