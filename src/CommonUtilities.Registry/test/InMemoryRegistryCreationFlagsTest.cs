using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public class InMemoryRegistryCreationFlagsTest
{
    [Fact]
    public void AssertValues()
    {
        Assert.Equal(0, (int)InMemoryRegistryCreationFlags.Default);
        Assert.Equal(InMemoryRegistryCreationFlags.WindowsLike,
            InMemoryRegistryCreationFlags.OnlyUseWindowsDataTypes |
            InMemoryRegistryCreationFlags.UseWindowsLengthLimits);
    }
}