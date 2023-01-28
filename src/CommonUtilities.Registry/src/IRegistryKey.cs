using System;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// High-Level abstraction layer for the a Registry Key implementation.
/// Read and write operations are supported.
/// </summary>
public interface IRegistryKey : IDisposable
{
    /// <summary>
    /// Retrieves the name of the key.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the view that was used to create the registry key.
    /// </summary>
    public RegistryView View { get; }

    /// <summary>
    /// Tries to get a value from a registry key.
    /// </summary>
    /// <typeparam name="T">The requested type of the key's value.</typeparam>
    /// <param name="name">The name of the key.</param>
    /// <param name="subPath">The sub path of the key.</param>
    /// <param name="result">The returned value or <paramref name="defaultValue"/> if no value could be found.</param>
    /// <param name="defaultValue">The default value, if no value could be found.</param>
    /// <returns><see langword="true"/> if a value was found; <see langword="false"/> otherwise.</returns>
    bool GetValueOrDefault<T>(string name, string subPath, out T? result, T? defaultValue);

    /// <summary>
    /// Tries to get a value from a registry key.
    /// </summary>
    /// <typeparam name="T">The requested type of the key's value.</typeparam>
    /// <param name="name">The name of the key.</param>
    /// <param name="result">The returned value or <paramref name="defaultValue"/> if no value could be found.</param>
    /// <param name="defaultValue"><see langword="true"/> if a value was found; <see langword="false"/> otherwise.</param>
    /// <returns><see langword="true"/> if a value was found; <see langword="false"/> otherwise.</returns>
    bool GetValueOrDefault<T>(string name, out T? result, T? defaultValue);

    /// <summary>
    /// Checks whether a given path exists in the registry.
    /// </summary>
    /// <param name="path">The requested path.</param>
    /// <returns><see langword="true"/> if a path exists; <see langword="false"/> otherwise.</returns>
    bool HasPath(string path);

    /// <summary>
    /// Checks whether a given key exists in the registry.
    /// </summary>
    /// <param name="name">The requested key.</param>
    /// <returns><see langword="true"/> if a key exists; <see langword="false"/> otherwise.</returns>
    bool HasValue(string name);

    /// <summary>
    /// Gets the value of a given key.
    /// </summary>
    /// <typeparam name="T">The requested type of the key's value.</typeparam>
    /// <param name="name">The name of the key.</param>
    /// <param name="subPath">The sub path of the key.</param>
    /// <param name="value">The returned value or <typeparamref name="T"/> default value if no value could be found.</param>
    /// <returns><see langword="true"/> if a value was found; <see langword="false"/> otherwise.</returns>
    bool GetValue<T>(string name, string subPath, out T? value);

    /// <summary>
    /// Gets the value of a given key.
    /// </summary>
    /// <typeparam name="T">The requested type of the key's value.</typeparam>
    /// <param name="name">The name of the key.</param>
    /// <param name="value">The returned value or <typeparamref name="T"/> default value if no value could be found.</param>
    /// <returns><see langword="true"/> if a value was found; <see langword="false"/> otherwise.</returns>
    bool GetValue<T>(string name, out T? value);

    /// <summary>
    /// Retrieves the value associated with the specified name. If the name is not found, returns the default value that you provide.
    /// </summary>
    /// <param name="name">The name of the value to retrieve. This string is not case-sensitive.</param>
    /// <param name="defaultValue">The value to return if <paramref name="name"/> does not exist.</param>
    /// <returns>The value associated with <paramref name="name"/>, with any embedded environment variables left unexpanded,
    /// or <paramref name="defaultValue"/> if <paramref name="name"/> is not found.</returns>
    object? GetValue(string? name, object? defaultValue);

    /// <summary>
    /// Gets the <see cref="IRegistryKey"/> of a given path.
    /// </summary>
    /// <param name="subPath">The sub-path.</param>
    /// <param name="writable">Set to <see langword="true"/> if write access is required. Default is <see langword="false"/>.</param>
    /// <returns>The registry key or <see langword="null"/> if the operation failed.</returns>
    IRegistryKey? GetKey(string subPath, bool writable = false);

    /// <summary>
    ///  Tries to get a value from a registry key. If the key did not exist, a key with a given default value gets created.
    /// </summary>
    /// <typeparam name="T">The requested type of the key's value.</typeparam>
    /// <param name="name">The name of the key.</param>
    /// <param name="defaultValue">The default value which shall be set</param>
    /// <param name="defaultValueUsed">Gets set to <see langword="true"/> if <paramref name="defaultValue"/> was set; <see langword="false"/> otherwise.</param>
    /// <returns>The actual or default value of the key.</returns>
    T? GetValueOrSetDefault<T>(string name, T? defaultValue, out bool defaultValueUsed);

    /// <summary>
    ///  Tries to get a value from a registry key. If the key did not exist, a key with a given default value gets created.
    /// </summary>
    /// <typeparam name="T">The requested type of the key's value.</typeparam>
    /// <param name="name">The name of the key.</param>
    /// <param name="subPath">The sub-path.</param>
    /// <param name="defaultValue">The default value which shall be set.</param>
    /// <param name="defaultValueUsed">Gets set to <see langword="true"/> if <paramref name="defaultValue"/> was set; <see langword="false"/> otherwise.</param>
    /// <returns>The actual or default value of the key.</returns>
    /// <exception cref="InvalidOperationException">If the requested registry key could not be found.</exception>
    T? GetValueOrSetDefault<T>(string name, string subPath, T? defaultValue, out bool defaultValueUsed);

    /// <summary>
    ///  Tries to write a value from a registry key.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <param name="value">The value which shall be set.</param>
    /// <returns><see langword="true"/> if a value was set successfully; <see langword="false"/> otherwise.</returns>
    bool WriteValue(string name, object value);

    /// <summary>
    /// Sets the specified name/value pair.
    /// </summary>
    /// <param name="name">The name of the value to store.</param>
    /// <param name="value">The data to be stored.</param>
    void SetValue(string? name, object value);

    /// <summary>
    ///  Tries to write a value from a registry key.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <param name="subPath">The sub-path.</param>
    /// <param name="value">The value which shall be set.</param>
    /// <returns><see langword="true"/> if a value was set successfully; <see langword="false"/> otherwise.</returns>
    bool WriteValue(string name, string subPath, object value);

    /// <summary>
    ///  Tries to delete a value from a registry key.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <returns><see langword="true"/> if a value was deleted successfully or didn't exist;
    /// <see langword="false"/> if the operation failed.</returns>
    bool DeleteValue(string name);

    /// <summary>
    ///  Tries to delete a value from a registry key.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <param name="subPath">The sub-path.</param>
    /// <returns><see langword="true"/> if a value was deleted successfully or didn't exist;
    /// <see langword="false"/> if the operation failed.</returns>
    bool DeleteValue(string name, string subPath);

    /// <summary>
    /// Deletes the specified <paramref name="subKey"/>.
    /// </summary>
    /// <param name="subKey">The name of the subkey to delete.</param>
    /// <param name="recursive">If set to <see langword="true"/>, deletes a subkey and any child subkeys recursively.</param>
    /// <returns><see langword="true"/> if the key was deleted successfully or didn't exist;
    /// <see langword="false"/> if the operation failed.</returns>
    /// <remarks>If <paramref name="subKey"/> is empty, the current instance will be disposed and deleted.</remarks>
    bool DeleteKey(string subKey, bool recursive);

    /// <summary>
    /// Retrieves an array of strings that contains all the subkey names or <see langword="null"/> if the operation failed.
    /// </summary>
    /// <param name="subPath">The sub-path.</param>
    /// <returns>An array of strings that contains the names of the subkeys for the given <paramref name="subPath"/>.</returns>
    string[]? GetSubKeyNames(string subPath);

    /// <summary>
    /// Creates a new subkey or opens an existing subkey.
    /// </summary>
    /// <param name="subKey">The name or path of the subkey to create or open. This string is not case-sensitive.</param>
    /// <returns>The newly created subkey, or <see langword="null"/> if the operation failed.
    /// If a zero-length string is specified for <paramref name="subKey"/>, the current <see cref="IRegistryKey"/> object is returned.</returns>
    IRegistryKey? CreateSubKey(string subKey);

    /// <summary>
    /// Retrieves an array of strings that contains all the subkey names or <see langword="null"/> if the operation failed.
    /// </summary>
    /// <returns>An array of strings that contains the names of the subkeys.</returns>
    string[] GetSubKeyNames();
}