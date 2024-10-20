using System;
using System.Globalization;
using System.Linq;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// Base implementation for an <inheritdoc cref="IRegistryKey"/>.
/// </summary>
public abstract class RegistryKeyBase : IRegistryKey
{
    /// <inheritdoc />
    public abstract bool IsCaseSensitive { get; }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract RegistryView View { get; }

    /// <inheritdoc />
    public abstract object? GetValue(string? name);

    /// <inheritdoc/>
    public abstract string[] GetSubKeyNames();

    /// <inheritdoc />
    public abstract string[] GetValueNames();

    /// <inheritdoc/>
    public abstract void SetValue(string? name, object value);

    /// <inheritdoc/>
    public abstract IRegistryKey? CreateSubKey(string subKey, bool writable = true);

    /// <inheritdoc/>
    public object? GetValue(string? name, object? defaultValue)
    {
        return GetValue(name) ?? defaultValue;
    }

    /// <inheritdoc />
    public T? GetValue<T>(string? name)
    {
        return GetValueOrDefault<T>(name, default, out _);
    }

    /// <inheritdoc />
    public T? GetValueOrDefault<T>(string? name, T? defaultValue, out bool valueExists)
    {
        var result = GetValue(name);
        if (result is null)
        {
            valueExists = false;
            return defaultValue;
        }

        valueExists = true;

        if (result is T t)
            return t;

        if (typeof(T).IsEnum)
            return (T)Enum.Parse(typeof(T), result.ToString());

        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }

    ///// <inheritdoc/>
    //public bool GetValueOrDefault<T>(string? name, string subPath, out T result, T? defaultValue)
    //{
    //    object? resultValue = defaultValue;
    //    var valueReceived = TryKeyOperation(subPath, key =>
    //    {
    //        var value = key?.GetValue(name);
    //        if (value is null)
    //            return false;
    //        try
    //        {
    //            if (value is T t)
    //            {
    //                resultValue = t;
    //                return true;
    //            }
    //            if (typeof(T).IsEnum)
    //            {
    //                resultValue = Enum.Parse(typeof(T), value.ToString());
    //                return true;
    //            }
    //            resultValue = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            return false;
    //        }
    //    }, false, false);
    //    result = (T) resultValue!;
    //    return valueReceived;
    //}

    /// <inheritdoc/>
    public bool HasPath(string subPath)
    {
        using var subKey = GetKey(subPath);
        return subKey is not null;
    }

    /// <inheritdoc/>
    public bool HasValue(string? name)
    {
        name ??= string.Empty;
        var comparer = IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        return GetValueNames().Contains(name, comparer);
    }

    /// <inheritdoc/>
    public abstract IRegistryKey? GetKey(string name, bool writable = false);
    
    /// <inheritdoc/>
    public T? GetValueOrSetDefault<T>(string? name, T? defaultValue, out bool defaultValueUsed)
    {
        var result = GetValueOrDefault(name, defaultValue, out var exists);

        defaultValueUsed = !exists;

        if (!exists && result is not null)
            SetValue(name, result);
        return result;
    }

    /// <inheritdoc/>
    public void DeleteValue(string? name)
    {
        DeleteValueCore(name);
    }
    
    /// <inheritdoc/>
    public void DeleteKey(string subPath, bool recursive)
    {
        if (subPath == null)
            throw new ArgumentNullException(nameof(subPath));
        DeleteKeyCore(subPath, recursive);
    }

    /// <inheritdoc cref="IDisposable"/>
    public virtual void Dispose()
    {
    }

    /// <summary>
    /// Retrieves a string representation of this key.
    /// </summary>
    /// <returns>A string representing the key. If the specified key is invalid (cannot be found) then <see langword="null"/> is returned.</returns>
    /// <exception cref="ObjectDisposedException">The <see cref="IRegistryKey"/> being accessed is closed (closed keys cannot be accessed).</exception>
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Deletes the specified subPath.
    /// </summary>
    /// <param name="subPath">The name of the subPath to delete.</param>
    /// <param name="recursive">If set to <see langword="true"/>, deletes the subPath and any child subkeys recursively.</param>
    protected abstract void DeleteKeyCore(string subPath, bool recursive);

    /// <summary>
    /// Deletes the given value.
    /// </summary>
    /// <param name="name">The name of the value to delete.</param>
    protected abstract void DeleteValueCore(string? name);
}