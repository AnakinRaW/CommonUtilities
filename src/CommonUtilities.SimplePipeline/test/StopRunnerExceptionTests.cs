using AnakinRaW.CommonUtilities.Testing;
using AnakinRaW.CommonUtilities.Testing.Extensions;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class StopRunnerExceptionTests : TestBaseWithServiceProvider
{
    [Fact]
    public void Ctor_Default_DoesNotThrow()
    {
        var ex = new StopRunnerException();
        Assert.Exception(ex, message: "Stopping step runner.");
    }
    
    [Fact]
    public void Ctor_WithMessage_SetsMessage()
    {
        var ex = new StopRunnerException("Test Message");
        Assert.Exception(ex, message: "Test Message");
    }
}