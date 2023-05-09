using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class CurrentProcessInfoTests
{
    [Fact]
    public void TestDefault()
    {
        Assert.Equal(0, default(CurrentProcessInfo).Id);
        Assert.Null(default(CurrentProcessInfo).ProcessFilePath);
    }

    [Fact]
    public void TestCurrentProcess()
    {
        var currentProcessInfo = CurrentProcessInfo.Current;
        Assert.NotEqual(0, currentProcessInfo.Id);
        Assert.NotNull(currentProcessInfo.ProcessFilePath);
    }
}