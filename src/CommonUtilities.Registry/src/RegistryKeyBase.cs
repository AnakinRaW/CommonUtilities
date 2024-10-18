using System;
using System.Globalization;

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

    /// <inheritdoc/>
    public abstract void SetValue(string? name, object value);

    /// <inheritdoc/>
    public abstract IRegistryKey? CreateSubKey(string subKey);

    /// <inheritdoc/>
    public object? GetValue(string? name, object? defaultValue)
    {
        return GetValue(name) ?? defaultValue;
    }

    /// <inheritdoc/>
    public bool GetValueOrDefault<T>(string? name, string subPath, out T result, T? defaultValue)
    {
        object? resultValue = defaultValue;
        var valueReceived = TryKeyOperation(subPath, key =>
        {
            var value = key?.GetValue(name);
            if (value is null)
                return false;
            try
            {
                if (value is T t)
                {
                    resultValue = t;
                    return true;
                }
                if (typeof(T).IsEnum)
                {
                    resultValue = Enum.Parse(typeof(T), value.ToString());
                    return true;
                }
                resultValue = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }, false, false);
        result = (T) resultValue!;
        return valueReceived;
    }

    /// <inheritdoc/>
    public bool GetValueOrDefault<T>(string? name, out T result, T? defaultValue)
    {
        return GetValueOrDefault(name, string.Empty, out result, defaultValue);
    }

    /// <inheritdoc/>
    public bool HasPath(string subPath)
    {
        return TryKeyOperation(subPath, key => key != null, false, false);
    }

    /// <inheritdoc/>
    public bool HasValue(string? name)
    {
        return GetValue<object>(name, out _);
    }

    /// <inheritdoc/>
    public bool GetValue<T>(string? name, string subPath, out T? value)
    {
        return GetValueOrDefault(name, subPath, out value, default);
    }

    /// <inheritdoc/>
    public bool GetValue<T>(string? name, out T? value)
    {
        return GetValue(name, string.Empty, out value);
    }

    /// <inheritdoc/>
    public IRegistryKey? GetKey(string name, bool writable = false)
    {
        if (name == null) 
            throw new ArgumentNullException(nameof(name));
        return name == string.Empty ? this : GetKeyCore(name, writable);
    }
    
    /// <inheritdoc/>
    public T GetValueOrSetDefault<T>(string? name, T? defaultValue, out bool defaultValueUsed)
    {
        return GetValueOrSetDefault(name, string.Empty, defaultValue, out defaultValueUsed);
    }

    /// <inheritdoc/>
    public T GetValueOrSetDefault<T>(string? name, string subPath, T? defaultValue, out bool defaultValueUsed)
    {
        if (subPath == null)
            throw new ArgumentNullException(nameof(subPath));
        defaultValueUsed = !GetValueOrDefault(name, subPath, out var value, defaultValue);
        if (defaultValueUsed)
        {
            if (defaultValue is null)
                return value;
            SetValue(name, subPath, defaultValue);
            return defaultValue;

        }
        return value;
    }
    
    /// <inheritdoc/>
    public bool SetValue(string? name, string subPath, object value)
    {
        if (subPath == null) 
            throw new ArgumentNullException(nameof(subPath));
        if (value == null) 
            throw new ArgumentNullException(nameof(value));
        return TryKeyOperation(subPath, key =>
        {
            if (key is null)
                return false;
            key.SetValue(name, value);
            return true;
        }, false, true);
    }

    /// <inheritdoc/>
    public bool DeleteValue(string name)
    {
        if (name == null) 
            throw new ArgumentNullException(nameof(name));
        return DeleteValue(name, string.Empty);
    }
    
    /// <inheritdoc/>
    public bool DeleteValue(string name, string subPath)
    {
        return TryKeyOperation(subPath, key =>
        {
            if (key is null)
                return true;
            if (key != this)
                return key.DeleteValue(name);
            DeleteValueCore(name);
            return true;
        }, false, true);
    }

    /// <inheritdoc/>
    public bool DeleteKey(string subPath, bool recursive)
    {
        if (subPath == null)
            throw new ArgumentNullException(nameof(subPath));
        return TryKeyOperation(subPath, key =>
        {
            if (key is null)
                return true;
            DeleteKeyCore(subPath, recursive);
            if (key == this)
                Dispose();
            return true;
        }, false, true);
    }

    /// <inheritdoc/>
    public string[]? GetSubKeyNames(string subPath)
    {
        if (subPath == null)
            throw new ArgumentNullException(nameof(subPath));
        return TryKeyOperation(subPath, key => key?.GetSubKeyNames(), null, false);
    }

    /// <inheritdoc />
    public bool DeleteKey()
    {
        return DeleteKey(string.Empty, true);
    }

    /// <inheritdoc cref="IDisposable"/>
    public virtual void Dispose()
    {
    }

    /// <inheritdoc />
    public string[]? GetValueNames()
    {
        throw new NotImplementedException();
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
    protected abstract void DeleteValueCore(string name);

    /// <summary>
    /// Gets the <see cref="IRegistryKey"/> of a given path.
    /// </summary>
    /// <param name="subPath">The sub-path.</param>
    /// <param name="writable">Set to <see langword="true"/> if write access is required. Default is <see langword="false"/>.</param>
    /// <returns>The registry key or <see langword="null"/> if the operation failed.</returns>
    protected abstract IRegistryKey? GetKeyCore(string subPath, bool writable);

    /// <summary>
    /// Tries to perform an operation on the specified sub path. 
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="subPath">The sub path to operate on.</param>
    /// <param name="operation">The actual operation action.</param>
    /// <param name="errorValue">The value that is returned in the case the operation failed.</param>
    /// <param name="writable">Indicates whether this operation requires write access to the registry.</param>
    /// <returns>The result of the operation or <paramref name="errorValue"/> if the operation failed.</returns>
    protected T TryKeyOperation<T>(string subPath, Func<IRegistryKey?, T> operation, T errorValue, bool writable)
    {
        if (subPath == null) 
            throw new ArgumentNullException(nameof(subPath));

        var key = GetKey(subPath, writable);
        try
        {
            return operation(key);
        }
        catch
        {
            return errorValue;
        }
        finally
        {
            if (key != this)
                key?.Dispose();
        }
    }
}