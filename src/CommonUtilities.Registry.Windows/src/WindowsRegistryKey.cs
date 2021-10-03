using System;
using Microsoft.Win32;
using Validation;

namespace Sklavenwalker.CommonUtilities.Registry.Windows;

/// <summary>
/// Windows specific RegistryKey implementation of <see cref="IRegistryKey"/>
/// </summary>
public class WindowsRegistryKey : RegistryKeyBase
{
    private RegistryKey _registryKey;

    /// <summary>
    /// Indicates whether this instance is disposed or not.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public override string Name => _registryKey.Name;

    /// <inheritdoc/>
    public override RegistryView View => ConvertView(_registryKey.View);

    /// <summary>
    /// Creates a new <inheritdoc cref="WindowsRegistryKey"/> from a given <see cref="RegistryKey"/>.
    /// </summary>
    /// <param name="registryKey">The internal registry key this instance represents.</param>
    public WindowsRegistryKey(RegistryKey registryKey)
    {
        Requires.NotNull(registryKey, nameof(registryKey));
        _registryKey = registryKey;
    }

    /// <summary>
    /// Finalizes this instance.
    /// </summary>
    ~WindowsRegistryKey() => Dispose(false);
    
    /// <inheritdoc/>
    public override object? GetValue(string? name, object? defaultValue)
    {
        return _registryKey.GetValue(name, defaultValue);
    }

    /// <inheritdoc/>
    protected override IRegistryKey? GetKeyCore(string subPath, bool writable)
    {
        var key = _registryKey.OpenSubKey(subPath!, writable);
        return key is null ? null : new WindowsRegistryKey(key);
    }

    /// <inheritdoc/>
    public override void SetValue(string? name, object value)
    {
        _registryKey.SetValue(name, value);
    }

    /// <inheritdoc/>
    public override IRegistryKey? CreateSubKey(string subKey)
    {
        var winKey = _registryKey.CreateSubKey(subKey);
        return winKey is null ? null : new WindowsRegistryKey(winKey);
    }

    /// <inheritdoc/>
    public override string[] GetSubKeyNames()
    {
        return _registryKey.GetSubKeyNames();
    }

    /// <inheritdoc cref="IDisposable"/>
    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;
        if (disposing)
        {
            _registryKey?.Dispose();
            _registryKey = null!;
        }
        IsDisposed = true;
    }

    internal static Microsoft.Win32.RegistryHive ConvertHive(RegistryHive hive)
    {
        return hive switch
        {
            RegistryHive.ClassesRoot => Microsoft.Win32.RegistryHive.ClassesRoot,
            RegistryHive.LocalMachine => Microsoft.Win32.RegistryHive.LocalMachine,
            RegistryHive.CurrentUser => Microsoft.Win32.RegistryHive.CurrentUser,
            _ => 0
        };
    }

    internal static RegistryView ConvertView(Microsoft.Win32.RegistryView view)
    {
        return view switch
        {
            Microsoft.Win32.RegistryView.Default => RegistryView.Default,
            Microsoft.Win32.RegistryView.Registry64 => RegistryView.Registry64,
            Microsoft.Win32.RegistryView.Registry32 => RegistryView.Registry32,
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, null)
        };
    }

    internal static Microsoft.Win32.RegistryView ConvertView(RegistryView view)
    {
        return view switch
        {
            RegistryView.Default => Microsoft.Win32.RegistryView.Default,
            RegistryView.Registry32 => Microsoft.Win32.RegistryView.Registry32,
            RegistryView.Registry64 => Microsoft.Win32.RegistryView.Registry64,
            _ => Microsoft.Win32.RegistryView.Default
        };
    }
}