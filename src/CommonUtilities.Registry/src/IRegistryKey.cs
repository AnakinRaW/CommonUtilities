using System;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// High-Level abstraction layer for the a Registry Key implementation.
/// Read and write operations are supported.
/// </summary>
public interface IRegistryKey : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether sub key paths and key value names are case-sensitive.
    /// </summary>
    public bool IsCaseSensitive { get; }

    /// <summary>
    /// Retrieves the name of the key.
    /// </summary>
    /// <remarks>
    /// The name of the key includes the absolute path of this key in the registry, always starting at a base key, for example, HKEY_LOCAL_MACHINE.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets the view that was used to create the registry key.
    /// </summary>
    public RegistryView View { get; }

    /// <summary>
    /// Determines whether the key contains a specified path of sub keys.
    /// </summary>
    /// <param name="subPath">The requested sub path.</param>
    /// <returns><see langword="true"/> if a subPath exists; <see langword="false"/> otherwise.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subPath"/> is <see langword="null"/>.</exception>
    bool HasPath(string subPath);

    /// <summary>
    /// Determines whether the key contains the specified value.
    /// </summary>
    /// <param name="name">The name of the value to check.</param>
    /// <returns><see langword="true"/> if a key exists; <see langword="false"/> otherwise.</returns>
    bool HasValue(string? name);

    /// <summary>
    /// Retrieves the value associated with the specified name. Returns <see langword="null"/> if the name/value pair does not exist in the registry.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <returns>The value associated with name, or <see langword="null"/> if name is not found.</returns>
    /// <remarks>
    /// A registry key can have one value that is not associated with any name.
    /// To retrieve this unnamed value, specify either <see langword="null"/> or the empty string ("") for <paramref name="name"/>.
    /// </remarks>
    object? GetValue(string? name);

    /// <summary>
    /// Retrieves the value associated with the specified name. If the name is not found, returns the default value that you provide.
    /// </summary>
    /// <param name="name">The name of the value to retrieve. This string is not case-sensitive.</param>
    /// <param name="defaultValue">The value to return if <paramref name="name"/> does not exist.</param>
    /// <returns>
    /// The value associated with <paramref name="name"/> or <paramref name="defaultValue"/> if <paramref name="name"/> is not found.
    /// </returns>
    /// <remarks>
    /// A registry key can have one value that is not associated with any name.
    /// To retrieve this unnamed value, specify either <see langword="null"/> or the empty string ("") for <paramref name="name"/>.
    /// </remarks>
    object? GetValue(string? name, object? defaultValue);

    /// <summary>
    /// Retrieves the value associated with the specified name.
    /// Returns default value of <typeparamref name="T"/> if the name/value pair does not exist in the registry.
    /// </summary>
    /// <typeparam name="T">The requested type of the value.</typeparam>
    /// <param name="name">The name of the value to retrieve.</param>
    /// <returns>
    /// The value associated with <paramref name="name"/>, or <see langword="default"/> if name is not found.
    /// </returns>
    /// <remarks>
    /// A registry key can have one value that is not associated with any name.
    /// To retrieve this unnamed value, specify either <see langword="null"/> or the empty string ("") for <paramref name="name"/>.
    /// </remarks>
    T? GetValue<T>(string? name);

    /// <summary>
    /// Retrieves the value associated with the specified name.
    /// If the name is not found, returns the default value that you provide.
    /// </summary>
    /// <typeparam name="T">The requested type of the name's value.</typeparam>
    /// <param name="name">The name of the key.</param>
    /// <param name="defaultValue"><see langword="true"/> if a value was found; <see langword="false"/> otherwise.</param>
    /// <param name="valueExists">When this method returns, <see langword="true"/> if the value was found; otherwise, <see langword="false"/>.</param>
    /// <returns><see langword="true"/> if a value was found; <see langword="false"/> otherwise.</returns>
    /// <remarks>
    /// A registry key can have one value that is not associated with any name.
    /// To retrieve this unnamed value, specify either <see langword="null"/> or the empty string ("") for <paramref name="name"/>.
    /// </remarks>
    T? GetValueOrDefault<T>(string? name, T? defaultValue, out bool valueExists);

    /// <summary>
    /// Retrieves the value associated with the specified name.
    /// If the value did not exist and <paramref name="defaultValue"/> is not <see langword="null"/>, the name/value pair is stored to the key.
    /// </summary>
    /// <typeparam name="T">The requested type of the key's value.</typeparam>
    /// <param name="name">The name of the key.</param>
    /// <param name="defaultValue">The default value which shall be set</param>
    /// <param name="defaultValueUsed">When this method returns, <see langword="true"/> if <paramref name="defaultValue"/> was used; otherwise, <see langword="false"/>.</param>
    /// <returns>The value associated with name, or <paramref name="defaultValue"/> if name is not found.</returns>
    /// <remarks>
    /// A registry key can have one value that is not associated with any name.
    /// To retrieve this unnamed value, specify either <see langword="null"/> or the empty string ("") for <paramref name="name"/>.
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">The <see cref="IRegistryKey"/> cannot be written to; for example, it was not opened as a writable key , or the user does not have the necessary access rights.</exception>
    T? GetValueOrSetDefault<T>(string? name, T? defaultValue, out bool defaultValueUsed);

    /// <summary>
    /// Sets the value of a name/value pair in the registry key.
    /// </summary>
    /// <param name="name">The name of the value to store.</param>
    /// <param name="value">The data to be stored.</param>
    /// <exception cref="UnauthorizedAccessException">The <see cref="IRegistryKey"/> cannot be written to; for example, it was not opened as a writable key , or the user does not have the necessary access rights.</exception>
    void SetValue(string? name, object value);

    /// <summary>
    ///  Tries to delete a value from a registry key.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    void DeleteValue(string? name);

    // TODO: Rename to OpenSubKey
    /// <summary>
    /// Retrieves a specified subkey, and specifies whether write access is to be applied to the key.
    /// </summary>
    /// <param name="name">The name or path of the subkey to open.</param>
    /// <param name="writable">Set to <see langword="true"/> if write access is required. Default is <see langword="false"/>.</param>
    /// <returns>The registry key or <see langword="null"/> if the subkey does not exist or the operation failed.</returns>
    /// <remarks>
    /// If the requested key does not exist, this method returns <see langword="null"/>.
    /// <hr/>
    /// <hr/>
    /// If <paramref name="writable"/> is  <see langword="true"/>, the key will be opened for reading and writing,
    /// otherwise, the key will be opened as read-only.
    /// <hr/>
    /// <hr/>
    /// To obtain the current RegistryKey object, specify an empty string ("") for <paramref name="name"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    IRegistryKey? GetKey(string name, bool writable = false);

    /// <summary>
    /// Creates a new subkey or opens an existing subkey with the specified access.
    /// </summary>
    /// <param name="subKey">The name or path of the subkey to create or open.</param>
    /// <param name="writable">
    /// <see langword="true"/> to indicate the new subkey is writable; otherwise, <see langword="false"/>.
    /// <see langword="true"/> is the default value.
    /// </param>
    /// <returns>The newly created subkey, or <see langword="null"/> if the operation failed.
    /// If a zero-length string is specified for <paramref name="subKey"/>, the current <see cref="IRegistryKey"/> object is returned.</returns>
    /// <remarks>To obtain the current RegistryKey object, specify an empty string ("") for <paramref name="subKey"/>.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="subKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="UnauthorizedAccessException">The <see cref="IRegistryKey"/> cannot be written to; for example, it was not opened as a writable key , or the user does not have the necessary access rights.</exception>
    IRegistryKey? CreateSubKey(string subKey, bool writable = true);

    /// <summary>
    /// Deletes the specified <paramref name="subKey"/>.
    /// </summary>
    /// <param name="subKey">The name of the subkey to delete.</param>
    /// <param name="recursive">If set to <see langword="true"/>, deletes a subkey and any child subkeys recursively.</param>
    void DeleteKey(string subKey, bool recursive);
    
    /// <summary>
    /// Retrieves an array of strings that contains all the value names associated with this key.
    /// </summary>
    /// <returns>
    /// An array of strings that contains the value names for the current key.
    /// </returns>
    /// <remarks>
    /// If no value names for the key are found, an empty array is returned.
    /// <br/>
    /// <br/>
    /// A registry key can have a default value - that is, a name/value pair in which the name is the empty string (""). If a default value has been set for a registry key, the array returned by the GetValueNames method includes the empty string.
    /// </remarks>
    string[] GetValueNames();

    /// <summary>
    /// Retrieves an array of strings that contains all the subkey names.
    /// </summary>
    /// <returns>An array of strings that contains the names of the subkeys.</returns>
    /// <remarks>This method does not recursively find names. It returns the names on the base level from which it was called.</remarks>
    string[] GetSubKeyNames();
}