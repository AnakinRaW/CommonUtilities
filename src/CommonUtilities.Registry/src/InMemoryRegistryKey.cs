using System;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// In memory <see cref="IRegistryKey"/> implementation for platform independent use. 
/// </summary>
public sealed class InMemoryRegistryKey : IRegistryKey
{
    private bool _disposed;

    internal readonly InMemoryRegistryKeyData KeyData;
    private readonly string _name;

    private bool IsWriteable { get; }

    /// <inheritdoc />
    public bool IsCaseSensitive => KeyData.IsCaseSensitive;

    /// <inheritdoc/>
    public string Name
    {
        get
        {
            ThrowIfDisposed();
            return _name;
        }
    }

    /// <inheritdoc/>
    public RegistryView View
    {
        get
        {
            ThrowIfDisposed();
            return KeyData.View;
        }
    }

    internal InMemoryRegistryKey(string name, InMemoryRegistryKeyData keyData, bool isWriteable)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        KeyData = keyData ?? throw new ArgumentNullException(nameof(keyData));
        IsWriteable = isWriteable;
    }

    /// <inheritdoc/>
    public bool HasPath(string subPath)
    {
        ThrowIfDisposed();
        return KeyData.HasPath(subPath);
    }

    /// <inheritdoc/>
    public bool HasValue(string? name)
    {
        ThrowIfDisposed();
        return KeyData.HasValue(name);
    }

    /// <inheritdoc/>
    public object? GetValue(string? name)
    {
        ThrowIfDisposed();
        return KeyData.GetValue(name);
    }

    /// <inheritdoc/>
    public object? GetValue(string? name, object? defaultValue)
    {
        ThrowIfDisposed();
        return KeyData.GetValue(name, defaultValue);
    }

    /// <inheritdoc />
    public T? GetValue<T>(string? name)
    {
        ThrowIfDisposed();
        return KeyData.GetValue<T>(name);
    }

    /// <inheritdoc />
    public T? GetValueOrDefault<T>(string? name, T? defaultValue, out bool valueExists)
    {
        ThrowIfDisposed();
        return KeyData.GetValueOrDefault(name, defaultValue, out valueExists);
    }


    /// <inheritdoc/>
    public T? GetValueOrSetDefault<T>(string? name, T? defaultValue, out bool defaultValueUsed)
    {
        ThrowIfDisposed();
        
        // TODO: Should be able to get the value even if not writeable
        ThrowIfNotWritable();
        return KeyData.GetValueOrSetDefault(name, defaultValue, out defaultValueUsed);
    }

    /// <inheritdoc/>
    public void SetValue(string? name, object value)
    {
        ThrowIfDisposed();
        ThrowIfNotWritable();
        KeyData.SetValue(name, value);
    }

    /// <inheritdoc/>
    public IRegistryKey? GetKey(string name, bool writable = false)
    {
        ThrowIfDisposed();
        return KeyData.GetKeyCore(_name, name, writable);
    }

    /// <inheritdoc />
    public IRegistryKey CreateSubKey(string subKey, bool writable = true)
    {
        ThrowIfDisposed();
        ThrowIfNotWritable();
        return KeyData.CreateSubKey(_name, subKey, writable);
    }

    /// <inheritdoc />
    public void DeleteValue(string? name)
    {
        ThrowIfDisposed();
        ThrowIfNotWritable();
        KeyData.DeleteValue(name);
    }

    /// <inheritdoc />
    public void DeleteKey(string subKey, bool recursive)
    {
        ThrowIfDisposed();
        ThrowIfNotWritable();
        KeyData.DeleteKey(subKey, recursive);
    }

    /// <inheritdoc/>
    public string[] GetValueNames()
    {
        ThrowIfDisposed();
        return KeyData.GetValueNames();
    }

    /// <inheritdoc/>
    public string[] GetSubKeyNames()
    {
        ThrowIfDisposed();
        return KeyData.GetSubKeyNames();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        ThrowIfDisposed();
        return KeyData.ToString();
    }

    /// <summary>
    /// Finalizes this instance.
    /// </summary>
    ~InMemoryRegistryKey() => Dispose(false);


    /// <inheritdoc cref="IDisposable"/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool _)
    {
        if (!KeyData.IsSystemKey)
            _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(KeyData.Name, "The registry key is already disposed.");
    }

    private void ThrowIfNotWritable()
    {
        if (!IsWriteable)
            throw new UnauthorizedAccessException("Cannot write to the registry key.");
    }
}