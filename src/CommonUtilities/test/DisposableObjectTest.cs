using Moq;
using System;
using System.Reflection;
using AnakinRaW.CommonUtilities.Testing;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class DisposableObjectTest
{
    [Fact]
    public void Test_DisposableObject_Lifecycle()
    {
        var disposable = new Mock<DisposableObject>
        {
            CallBase = true
        };

        Assert.False(disposable.Object.IsDisposed);

        var throwIfDisposed = disposable.Object.GetType().GetMethod("ThrowIfDisposed", BindingFlags.NonPublic | BindingFlags.Instance);

        // Must not throw!
        throwIfDisposed!.Invoke(disposable.Object, null);

        disposable.Object.Dispose();

        disposable.Protected().Verify("DisposeResources", Times.Once());

        Assert.True(disposable.Object.IsDisposed);

        // Disposing again
        disposable.Object.Dispose();

        Assert.True(disposable.Object.IsDisposed);

        disposable.Protected().Verify("DisposeResources", Times.Once());

        AssertExtensions.Throws_IgnoreTargetInvocationException<ObjectDisposedException>(() =>
            throwIfDisposed!.Invoke(disposable.Object, null));
    }
}