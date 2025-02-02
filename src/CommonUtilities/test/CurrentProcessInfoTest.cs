using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class CurrentProcessInfoTests
{
    [Fact]
    public void Current()
    {
        var currentProcessInfo = CurrentProcessInfo.Current;
        Assert.NotEqual(0, currentProcessInfo.Id);
        Assert.NotNull(currentProcessInfo.ProcessFilePath);
    }
}