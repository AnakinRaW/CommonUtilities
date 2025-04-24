using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Process;

public class CurrentProcessInfoProviderTest
{
    [Fact]
    public void GetCurrentProcessInfo()
    {
        var p1 = new CurrentProcessInfoProvider();
        var cpi1 = p1.GetCurrentProcessInfo();

        var cpi2 = CurrentProcessInfo.Current;

        Assert.Same(cpi1, cpi2);

        var p2 = new CurrentProcessInfoProvider();
        var cpi3 = p2.GetCurrentProcessInfo();
        Assert.Same(cpi3, cpi2);
    }
}