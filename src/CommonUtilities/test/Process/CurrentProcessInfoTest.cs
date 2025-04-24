using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Process;

public class CurrentProcessInfoTest
{
    [Fact]
    public void Current_IsSingleton()
    {
        var cpi1 = CurrentProcessInfo.Current;
        var cpi2 = CurrentProcessInfo.Current;
        Assert.Same(cpi1, cpi2);
    }

    [Fact]
    public void ProcessFilePath()
    {
        var cpi = CurrentProcessInfo.Current;
        var id = cpi.Id;
        var current = System.Diagnostics.Process.GetProcessById(id);
        Assert.NotNull(current);

        Assert.Equal(current.MainModule!.FileName, cpi.ProcessFilePath);
    }

    [Fact]
    public void IsElevated()
    {
        var cpi = CurrentProcessInfo.Current;
        Assert.False(cpi.IsElevated);
    }
}