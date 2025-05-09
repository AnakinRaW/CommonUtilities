﻿using System;
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

        try
        {
            // We already know that the result is not null.
            var type = typeof(T);
            var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;

            if (nonNullableType.IsEnum)
                return (T)Enum.Parse(nonNullableType, result.ToString(), true);

            return (T)Convert.ChangeType(result, nonNullableType, CultureInfo.InvariantCulture);
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Unable to convert registry value '{result}' to type {typeof(T)}", e);
        }
    }

    /// <inheritdoc/>
    public bool HasPath(string subPath)
    {
        using var subKey = OpenSubKey(subPath);
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
    public abstract IRegistryKey? OpenSubKey(string name, bool writable = false);
    
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