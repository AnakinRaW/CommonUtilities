using System.Collections.Generic;
using System.Linq;

namespace Sklavenwalker.CommonUtilities.Registry;

/// <summary>
/// In memory <see cref="IRegistryKey"/> implementation for platform independent use. 
/// </summary>
public class InMemoryRegistryKey : RegistryKeyBase
{
    private const char Separator = '\\';

    private readonly Dictionary<string, InMemoryRegistryKey> _subKeys = new();
    private readonly Dictionary<string, object> _values = new();

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override RegistryView View { get; }

    /// <summary>
    /// Creates a new instance of an in memory RegistryKey.
    /// </summary>
    /// <param name="view">The registry view this key is associated with.</param>
    /// <param name="name">The name of the key.</param>
    public InMemoryRegistryKey(RegistryView view, string name)
    {
        View = view;
        Name = name;
    }

    /// <inheritdoc/>
    public override object? GetValue(string? name, object? defaultValue)
    {
        var valueName = name ?? string.Empty;
        return _values.TryGetValue(valueName, out var value) ? value : defaultValue;
    }

    /// <inheritdoc/>
    public override void SetValue(string? name, object value)
    {
        var valueName = name ?? string.Empty;
        _values[valueName] = value;
    }

    /// <inheritdoc />
    public override IRegistryKey? CreateSubKey(string subKey)
    {
        try
        {
            var currentKey = this;
            var subKeyNames = subKey.Split(Separator);
            foreach (var subKeyName in subKeyNames)
            {
                var subKeyNameLower = subKeyName.ToLower();
                if (!currentKey._subKeys.ContainsKey(subKeyNameLower))
                    currentKey._subKeys[subKeyNameLower] = new InMemoryRegistryKey(View, subKeyName);
                currentKey = currentKey._subKeys[subKeyNameLower];
            }

            return currentKey;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public override string[] GetSubKeyNames()
    {
        return _subKeys.Values.Select(v => v.Name).ToArray();
    }

    /// <inheritdoc/>
    protected override IRegistryKey? GetKeyCore(string subPath, bool writable)
    {
        var currentKey = this;
        var subKeyNames = subPath.Split(Separator);
        foreach (var subKeyName in subKeyNames)
        {
            var subKeyNameLower = subKeyName.ToLower();
            if (currentKey._subKeys.ContainsKey(subKeyNameLower) == false)
                return null;
            currentKey = currentKey._subKeys[subKeyNameLower];
        }

        return currentKey;
    }
}