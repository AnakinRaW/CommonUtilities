using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// Internal, persistent data store for an <see cref="InMemoryRegistryKey"/>
/// </summary>
internal sealed class InMemoryRegistryKeyData : RegistryKeyBase
{
    private const int MaxKeyLength = 255;
    private const int MaxValueLength = 16_383;
    private const char Separator = '\\';

    private readonly Dictionary<string, InMemoryRegistryKeyData> _subKeys;
    private readonly Dictionary<string, object> _values;
    private readonly InMemoryRegistryCreationFlags _flags;

    private readonly bool _usePathLimit;
    private readonly bool _useTypeLimit;

    private InMemoryRegistryKeyData? _parent;

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
        InMemoryRegistryCreationFlags flags, 
        bool isSystemKey)
    {
        if (subName is null)
            throw new ArgumentNullException(nameof(subName));
        if (subName.Length == 0)
            throw new ArgumentException(nameof(subName));
        if (parent is null && !isSystemKey)
            throw new InvalidOperationException("Root keys must be marked as system keys!");

        var isCaseSensitive = flags.HasFlag(InMemoryRegistryCreationFlags.CaseSensitive);
        var stringComparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        
        _subKeys = new Dictionary<string, InMemoryRegistryKeyData>(stringComparer);
        _values = new Dictionary<string, object>(stringComparer);
        _parent = parent;
        _flags = flags;
        _usePathLimit = flags.HasFlag(InMemoryRegistryCreationFlags.UseWindowsLengthLimits);
        _useTypeLimit = flags.HasFlag(InMemoryRegistryCreationFlags.OnlyUseWindowsDataTypes);

        Name = BuildFromHierarchyName(parent, subName);
        SubName = subName;
        View = view;
        IsCaseSensitive = isCaseSensitive;
        IsSystemKey = isSystemKey;
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
        
        name ??= string.Empty;

        if (_usePathLimit && name.Length > MaxValueLength)
            throw new ArgumentException("Registry value names should not be greater than 16,383 characters.");

        if (_useTypeLimit)
            ValidateType(value);

        _values[name] = value;
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
        var key = (InMemoryRegistryKey?)OpenSubKey(fixedPath, writable: false);
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
            keyToDelete._parent = null;
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

        // The Windows implementation first performs the length check and then normalizes the subKey
        if (_usePathLimit)
            ValidateKeyName(subKey);

        subKey = FixupName(subKey);

        if (subKey == string.Empty)
            return new InMemoryRegistryKey(BuildSubKeyName(currentName, subKey), this, writable);

        var subKeyNames = subKey.Split(Separator);
        var currentKey = this;

        foreach (var subKeyName in subKeyNames)
        {
            if (!currentKey._subKeys.ContainsKey(subKeyName))
                currentKey._subKeys[subKeyName] = new InMemoryRegistryKeyData(View, subKeyName, currentKey, currentKey._flags, false);
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
    public override IRegistryKey? OpenSubKey(string subPath, bool writable = false)
    {
        if (subPath == null)
            throw new ArgumentNullException(nameof(subPath));
        return GetKeyCore(Name, subPath, writable);
    }

    internal IRegistryKey? GetKeyCore(string currentName, string subPath, bool writable)
    {
        if (subPath == null)
            throw new ArgumentNullException(nameof(subPath));

        // The Windows implementation first performs the length check and then normalizes the subKey
        if (_usePathLimit)
            ValidateKeyName(subPath);

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
        if (IsSystemKey)
            return true;
        return _parent is not null;
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
    private static void ValidateKeyName([NotNull] string? name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        var nextSlash = name.IndexOf("\\", StringComparison.OrdinalIgnoreCase);
        var current = 0;
        while (nextSlash != -1)
        {
            if (nextSlash - current > MaxKeyLength)
                throw new ArgumentException();
            current = nextSlash + 1;
            nextSlash = name.IndexOf("\\", current, StringComparison.OrdinalIgnoreCase);
        }

        if (name.Length - current > MaxKeyLength)
            throw new ArgumentException();
    }

    private static void ValidateType(object value)
    {
        if (value is not Array or byte[])
            return;
        if (value is string[] stringArr)
        {
            foreach (var s in stringArr)
            {
                if (s is null)
                    throw new ArgumentException("RegistryKey.SetValue does not allow a String[] that contains a null String reference.");
            }

            return;
        }
        throw new ArgumentException($"RegistryKey.SetValue does not support arrays of type '{value.GetType()}'. Only Byte[] and String[] are supported.");
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