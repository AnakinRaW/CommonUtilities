using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class DisposableObjectTest
{
    [Fact]
    public void DisposeTest()
    {
        var obj = new MockDisposableObject();
        Assert.False(obj.IsDisposed);
            
        var eventFlag = false;
        obj.Disposing += (_, _) => eventFlag = true;
        obj.Dispose();
        Assert.True(eventFlag);
        Assert.True(obj.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => obj.Disposing += (_, _) => {});
            
    }

    private class MockDisposableObject : DisposableObject
    {
    }
}