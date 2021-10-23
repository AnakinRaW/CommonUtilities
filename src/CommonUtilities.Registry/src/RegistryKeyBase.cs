using System;

namespace Sklavenwalker.CommonUtilities.Registry;

/// <summary>
/// Base implementation for an <inheritdoc cref="IRegistryKey"/>.
/// </summary>
public abstract class RegistryKeyBase : IRegistryKey
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract RegistryView View { get; }

    /// <inheritdoc/>
    public bool GetValueOrDefault<T>(string name, string subPath, out T? result, T? defaultValue)
    {
        result = defaultValue;
        var key = GetKey(subPath);
        var value = key?.GetValue(name, defaultValue);
        if (key is not null && key != this)
            key.Dispose();
        if (value is null)
            return false;

        try
        {
            result = (T)value;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public abstract object? GetValue(string? name, object? defaultValue);

    /// <inheritdoc/>
    public abstract string[] GetSubKeyNames();

    /// <inheritdoc/>
    public abstract void SetValue(string? name, object value);

    /// <inheritdoc/>
    public bool GetValueOrDefault<T>(string name, out T? result, T? defaultValue)
    {
        return GetValueOrDefault(name, string.Empty, out result, defaultValue);
    }

    /// <inheritdoc/>
    public bool HasPath(string path)
    {
        using var key = GetKey(path);
        return key != null;
    }

    /// <inheritdoc/>
    public bool HasValue(string name)
    {
        return GetValue<object>(name, out _);
    }

    /// <inheritdoc/>
    public bool GetValue<T>(string name, string subPath, out T? value)
    {
        return GetValueOrDefault(name, subPath, out value, default);
    }

    /// <inheritdoc/>
    public bool GetValue<T>(string name, out T? value)
    {
        return GetValue(name, string.Empty, out value);
    }

    /// <inheritdoc/>
    public IRegistryKey? GetKey(string? subPath, bool writable = false)
    {
        return string.IsNullOrEmpty(subPath) ? this : GetKeyCore(subPath!, writable);
    }
    
    /// <inheritdoc/>
    public T? GetValueOrSetDefault<T>(string name, T? defaultValue, out bool defaultValueUsed)
    {
        return GetValueOrSetDefault(name, string.Empty, defaultValue, out defaultValueUsed);
    }

    /// <inheritdoc/>
    public T? GetValueOrSetDefault<T>(string name, string subPath, T? defaultValue, out bool defaultValueUsed)
    {
        defaultValueUsed = !GetValue<T>(name, subPath, out var value);
        if (defaultValueUsed)
        {
            if (defaultValue is null)
                return value;
            WriteValue(name, subPath, defaultValue);
            return defaultValue;

        }
        return value;
    }

    /// <inheritdoc/>
    public bool WriteValue(string name, object value)
    {
        return WriteValue(name, string.Empty, value);
    }
    
    /// <inheritdoc/>
    public bool WriteValue(string name, string subPath, object value)
    {
        try
        {
            var key = GetKey(subPath, true);
            if (key is null)
                return false;
            key.SetValue(name, value);
            if (key != this)
                key.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool DeleteValue(string name)
    {
        return DeleteValue(name, string.Empty);
    }

    /// <inheritdoc/>
    public bool DeleteValue(string name, string subPath)
    {
        try
        {
            using var key = GetKey(subPath, true);
            if (key is null)
                return false;
            key.DeleteValue(name);
            if (key != this)
                key.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public abstract IRegistryKey? CreateSubKey(string subKey);

    /// <inheritdoc/>
    public string[]? GetSubKeyNames(string subPath)
    {
        using var key = GetKey(subPath);
        if (key is null)
            return null;
        try
        {
            return key.GetSubKeyNames();
        }
        catch
        {
            return null;
        }
        finally
        {
            if (key != this)
                key.Dispose();
        }
    }

    /// <inheritdoc cref="IDisposable"/>
    public virtual void Dispose()
    {
    }

    /// <summary>
    /// Gets the <see cref="IRegistryKey"/> of a given path.
    /// </summary>
    /// <param name="subPath">The sub-path.</param>
    /// <param name="writable">Set to <see langword="true"/> if write access is required. Default is <see langword="false"/>.</param>
    /// <returns>The registry key or <see langword="null"/> if the operation failed.</returns>
    protected abstract IRegistryKey? GetKeyCore(string subPath, bool writable);
}