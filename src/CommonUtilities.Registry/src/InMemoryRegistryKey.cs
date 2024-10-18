using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// In memory <see cref="IRegistryKey"/> implementation for platform independent use. 
/// </summary>
public sealed class InMemoryRegistryKey : RegistryKeyBase
{
    private const char Separator = '\\';

    private readonly Dictionary<string?, InMemoryRegistryKey> _subKeys;
    private readonly Dictionary<string?, object> _values;
    private readonly InMemoryRegistryKey? _parent;

    /// <summary>
    /// Gets the name of key without the full qualified path.
    /// </summary>
    public string SubName { get; }

    /// <inheritdoc />
    public override bool IsCaseSensitive { get; }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override RegistryView View { get; }

    internal InMemoryRegistryKey(RegistryView view, string subName, InMemoryRegistryKey? parent, bool isCaseSensitive)
    {
        IsCaseSensitive = isCaseSensitive;

        var stringComparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        _subKeys = new Dictionary<string?, InMemoryRegistryKey>(stringComparer);
        _values = new Dictionary<string?, object>(stringComparer);

        View = view;
        SubName = subName;
        _parent = parent;
        Name = BuildName(this, new StringBuilder(SubName));
    }

    /// <inheritdoc />
    public override object? GetValue(string? name)
    {
        var valueName = name ?? string.Empty;
        return _values.TryGetValue(valueName, out var value) ? value : null;
    }

    /// <inheritdoc/>
    public override void SetValue(string? name, object value)
    {
        if (value is null) 
            throw new ArgumentNullException(nameof(value));
        var valueName = name ?? string.Empty;
        _values[valueName] = value;
    }

    /// <inheritdoc/>
    protected override void DeleteValueCore(string name)
    {
        _values.Remove(name);
    }

    /// <inheritdoc/>
    protected override void DeleteKeyCore(string subPath, bool recursive)
    {
        // The calling method already assures the key exists.
        var key = (InMemoryRegistryKey)GetKeyCore(subPath, false)!;

        if (key._subKeys.Any() && !recursive)
            throw new InvalidOperationException();

        var keyQueue = new Queue<InMemoryRegistryKey>();
        keyQueue.Enqueue(key);
        while (keyQueue.Any())
        {
            var keyToDelete = keyQueue.Dequeue();
            foreach (var inMemoryRegistryKey in keyToDelete._subKeys.Values) 
                keyQueue.Enqueue(inMemoryRegistryKey);
            keyToDelete._subKeys.Clear();
            keyToDelete._values.Clear();
        }

        if (key == this && _parent is not null)
            _parent._subKeys.Remove(SubName);

        _subKeys.Clear();
    }

    /// <inheritdoc />
    public override IRegistryKey? CreateSubKey(string subKey)
    {
        if (subKey == null) 
            throw new ArgumentNullException(nameof(subKey));
        if (subKey == string.Empty)
            return this;

        try
        {
            var currentKey = this;
            var subKeyNames = subKey.Split(Separator);
            foreach (var subKeyName in subKeyNames)
            {
                if (!currentKey._subKeys.ContainsKey(subKeyName))
                    currentKey._subKeys[subKeyName] = new InMemoryRegistryKey(View, subKeyName, currentKey, currentKey.IsCaseSensitive);
                currentKey = currentKey._subKeys[subKeyName];
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
        return _subKeys.Values.Select(v => v.SubName).ToArray();
    }

    /// <inheritdoc/>
    protected override IRegistryKey? GetKeyCore(string subPath, bool writable)
    {
        if (subPath == string.Empty)
            return this;

        var currentKey = this;
        var subKeyNames = subPath.Split(Separator);
        foreach (var subKeyName in subKeyNames)
        {
            if (currentKey._subKeys.TryGetValue(subKeyName, out var key) == false)
                return null;
            currentKey = key;
        }

        return currentKey;
    }

    private static string BuildName(InMemoryRegistryKey key, StringBuilder sb)
    {
        if (key._parent is null)
            return sb.ToString();
        sb.Insert(0, key._parent.SubName + "\\");
        return BuildName(key._parent, sb);
    }
}