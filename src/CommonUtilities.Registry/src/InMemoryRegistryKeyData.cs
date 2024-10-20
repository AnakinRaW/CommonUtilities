using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// Internal, persistent data store for an <see cref="InMemoryRegistryKey"/>
/// </summary>
internal sealed class InMemoryRegistryKeyData : RegistryKeyBase
{
    private const char Separator = '\\';

    private readonly Dictionary<string, InMemoryRegistryKeyData> _subKeys;
    private readonly Dictionary<string, object> _values;
    private readonly InMemoryRegistryKeyData? _parent;

    internal bool IsSystemKey { get; }

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

    internal InMemoryRegistryKeyData(
        RegistryView view, 
        string subName, 
        InMemoryRegistryKeyData? parent,
        bool isCaseSensitive, 
        bool isSystemKey)
    {
        IsCaseSensitive = isCaseSensitive;
        IsSystemKey = isSystemKey;

        var stringComparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        _subKeys = new Dictionary<string, InMemoryRegistryKeyData>(stringComparer);
        _values = new Dictionary<string, object>(stringComparer);

        View = view;
        SubName = subName;
        _parent = parent;
        Name = BuildFromHierarchyName(parent, SubName);
    }

    /// <inheritdoc />
    public override object? GetValue(string? name)
    {
        var valueName = name ?? string.Empty;
        return _values.TryGetValue(valueName, out var value) ? value : null;
    }

    public override string[] GetValueNames()
    {
        ThrowIfNotExist();
        return _values.Keys.ToArray();
    }

    /// <inheritdoc/>
    public override void SetValue(string? name, object value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        ThrowIfNotExist();

        var valueName = name ?? string.Empty;
        _values[valueName] = value;
    }

    /// <inheritdoc/>
    protected override void DeleteValueCore(string? name)
    {
        name ??= string.Empty;
        _values.Remove(name);
    }

    /// <inheritdoc/>
    protected override void DeleteKeyCore(string subPath, bool recursive)
    {
        var fixedPath = FixupName(subPath);

        // The calling method already assures the key exists.
        var key = (InMemoryRegistryKey?)GetKey(fixedPath, writable: false);
        if (key == null)
            return;

        var keyData = key.KeyData;

        if (keyData._subKeys.Any() && !recursive)
            throw new InvalidOperationException();


        if (keyData.IsSystemKey)
        {
            // Need to make distinction here to mimic Windows behavior
            if (subPath.Length > 0)
                throw new UnauthorizedAccessException();
            throw new ArgumentException();
        }

        var keyQueue = new Queue<InMemoryRegistryKeyData>();
        keyQueue.Enqueue(keyData);
        while (keyQueue.Any())
        {
            var keyToDelete = keyQueue.Dequeue();
            foreach (var inMemoryRegistryKey in keyToDelete._subKeys.Values)
                keyQueue.Enqueue(inMemoryRegistryKey);
            keyToDelete._subKeys.Clear();
            keyToDelete._values.Clear();
            keyToDelete._parent?._subKeys.Remove(keyToDelete.SubName);
        }
    }

    /// <inheritdoc />
    public override IRegistryKey CreateSubKey(string subKey, bool writable = true)
    {
        return CreateSubKey(Name, subKey, writable);
    }

    internal IRegistryKey CreateSubKey(string currentName, string subKey, bool writable = true)
    {
        if (currentName == null) 
            throw new ArgumentNullException(nameof(currentName));
        if (subKey == null)
            throw new ArgumentNullException(nameof(subKey));

        ThrowIfNotExist();

        var currentKey = this;

        subKey = FixupName(subKey);

        if (subKey == string.Empty)
            return new InMemoryRegistryKey(BuildSubKeyName(currentName, subKey), this, writable);

        var subKeyNames = subKey.Split(Separator);
        foreach (var subKeyName in subKeyNames)
        {
            if (!currentKey._subKeys.ContainsKey(subKeyName))
                currentKey._subKeys[subKeyName] = new InMemoryRegistryKeyData(View, subKeyName, currentKey, currentKey.IsCaseSensitive, false);
            currentKey = currentKey._subKeys[subKeyName];
        }

        return new InMemoryRegistryKey(BuildSubKeyName(currentName, subKey), currentKey, writable);
    }


    /// <inheritdoc/>
    public override string[] GetSubKeyNames()
    {
        ThrowIfNotExist();
        return _subKeys.Count == 0 ? [] : _subKeys.Values.Select(v => v.SubName).ToArray();
    }

    public override string ToString()
    {
        return Name;
    }

    public override void Dispose()
    {
        // Never dispose, as this is the real storage object.
    }

    /// <inheritdoc/>
    public override IRegistryKey? GetKey(string subPath, bool writable = false)
    {
        if (subPath == null)
            throw new ArgumentNullException(nameof(subPath));
        return GetKeyCore(Name, subPath, writable);
    }

    internal IRegistryKey? GetKeyCore(string currentName, string subPath, bool writable)
    {
        if (subPath == null)
            throw new ArgumentNullException(nameof(subPath));

        subPath = FixupName(subPath);

        if (subPath == string.Empty)
            return Exists() ? new InMemoryRegistryKey(BuildSubKeyName(currentName, subPath), this, writable) : null;

        var currentKey = this;
        var subKeyNames = subPath.Split(Separator);
        foreach (var subKeyName in subKeyNames)
        {
            if (currentKey._subKeys.TryGetValue(subKeyName, out var key) == false)
                return null;
            currentKey = key;
        }

        return new InMemoryRegistryKey(BuildSubKeyName(currentName, subPath), currentKey, writable);
    }

    private bool Exists()
    {
        return _parent is null || _parent._subKeys.ContainsKey(SubName);
    }

    private static string BuildFromHierarchyName(InMemoryRegistryKeyData? parent, string subName)
    {
        if (parent is null)
            return subName;

        return parent.Name + "\\" + subName;
    }

    private static string BuildSubKeyName(string currentName, string subKey)
    {
        return currentName + "\\" + subKey;
    }

    private void ThrowIfNotExist()
    {
        if (!Exists())
            throw new IOException("Illegal operation attempted on a registry key that has been marked for deletion.");
    }

    // Shamelessly copied from https://github.com/dotnet/runtime
    private static string FixupName(string name)
    {
        if (!name.Contains('\\'))
            return name;

        var sb = new StringBuilder(name);

        FixupPath(sb);
        var temp = sb.Length - 1;
        if (temp >= 0 && sb[temp] == '\\') // Remove trailing slash
            sb.Length = temp;

        return sb.ToString();
    }


    // Shamelessly copied from https://github.com/dotnet/runtime
    private static void FixupPath(StringBuilder path)
    {
        var length = path.Length;
        var fixup = false;
        const char markerChar = (char)0xFFFF;

        var i = 1;
        while (i < length - 1)
        {
            if (path[i] == '\\')
            {
                i++;
                while (i < length && path[i] == '\\')
                {
                    path[i] = markerChar;
                    i++;
                    fixup = true;
                }
            }
            i++;
        }

        if (fixup)
        {
            i = 0;
            var j = 0;
            while (i < length)
            {
                if (path[i] == markerChar)
                {
                    i++;
                    continue;
                }
                path[j] = path[i];
                i++;
                j++;
            }
            path.Length += j - i;
        }
    }
}