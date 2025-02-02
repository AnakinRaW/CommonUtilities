using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class DisposableObjectTest
{
    [Fact]
    public void DisposableObject_Lifecycle()
    {
        var disposeCounter = 0;
        var disposable = new TestDisposable(() =>
        {
            disposeCounter++;
        });

        var eventCounter = 0;
        disposable.Disposing += (sender, args) =>
        {
            Assert.Same(disposable, sender);
            eventCounter++;
        };

        Assert.False(disposable.IsDisposed);
        Assert.Equal(0, disposeCounter);
        Assert.Equal(0, eventCounter);

        
        // Must not throw!
        disposable.ExposeThrowIfDisposed();

        Assert.False(disposable.IsDisposed);
        Assert.Equal(0, disposeCounter);
        Assert.Equal(0, eventCounter);

        disposable.Dispose();
        
        Assert.True(disposable.IsDisposed);
        Assert.Equal(1, disposeCounter);
        Assert.Equal(1, eventCounter);

        // Disposing again
        disposable.Dispose();

        Assert.True(disposable.IsDisposed);
        Assert.Equal(1, disposeCounter);
        Assert.Equal(1, eventCounter);
    }

    private class TestDisposable(Action onDispose) : DisposableObject
    {
        public void ExposeThrowIfDisposed()
        {
            ThrowIfDisposed();
        }

        protected override void DisposeResources()
        {
            onDispose();
            base.DisposeResources();
        }
    }
}