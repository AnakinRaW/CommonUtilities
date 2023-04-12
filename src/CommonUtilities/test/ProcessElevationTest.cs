using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class ProcessElevationTest
{
    [Fact]
    public void TestNotElevated()
    {
        Assert.False(ProcessElevation.Default.IsCurrentProcessElevated);
    }
}