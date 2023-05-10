using AnakinRaW.CommonUtilities.Process;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class CurrentProcessInfoTests
{
    [Fact]
    public void TestCurrentProcess()
    {
        var currentProcessInfo = CurrentProcessInfo.Current;
        Assert.NotEqual(0, currentProcessInfo.Id);
        Assert.NotNull(currentProcessInfo.ProcessFilePath);
    }
}