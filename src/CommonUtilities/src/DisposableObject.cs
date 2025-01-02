using System;

namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Base implementation for classes which shall implement the <see cref="IDisposable"/> interface.
/// This class provides convenience like an event, a status flag and validation method.
/// <br/>
/// Disposable resources are divided in managed resources and unmanaged resources.
/// Managed resources get disposed when explicitly calling <see cref="Dispose()"/> on the instance.
/// Unmanaged resources additionally get disposed when the instance is finalized by the GC.
/// </summary>
public abstract class DisposableObject : IDisposable
{
    private EventHandler? _disposing;

    /// <summary>
    /// Raised when the event is being disposed, while it is still accessible.
    /// <remarks>The event is not triggered when the object is finalized by the GC.</remarks>
    /// </summary>
    public event EventHandler Disposing
    {
        add
        {
            ThrowIfDisposed();
            _disposing += value;
        }
        remove => _disposing -= value;
    }

    /// <summary>
    /// Returns whether the object has been disposed once, protects against double disposal
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc cref="Finalize"/>
    ~DisposableObject()
    {
        Dispose(false);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this object has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>
    /// Disposes this instance and frees managed and unmanaged resources.
    /// Once this method is called <see cref="IsDisposed"/> is set to <see langword="true"/>
    /// </summary>
    /// <param name="disposing">When set to <see langword="true"/> managed resources get disposed.</param>
    protected void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;
        try
        {
            if (disposing)
            {
                _disposing?.Invoke(this, EventArgs.Empty);
                _disposing = null;
                DisposeManagedResources();
            }
            DisposeNativeResources();
        }
        finally
        {
            IsDisposed = true;
        }
    }

    /// <summary>
    /// Allows derived classes to provide custom dispose handling for managed resources.
    /// </summary>
    protected virtual void DisposeManagedResources()
    {
    }

    /// <summary>
    /// Allows derived classes to provide custom dispose handling for native resources.
    /// </summary>
    protected virtual void DisposeNativeResources()
    {
    }
}