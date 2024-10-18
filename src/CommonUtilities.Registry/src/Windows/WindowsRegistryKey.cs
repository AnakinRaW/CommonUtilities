using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
#if NET8_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AnakinRaW.CommonUtilities.Registry.Windows;

/// <summary>
/// Windows specific RegistryKey implementation of <see cref="IRegistryKey"/>
/// </summary>
#if NET8_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public sealed class WindowsRegistryKey : RegistryKeyBase
{
    /// <summary>
    /// Returns the underlying <see cref="RegistryKey"/> of this instance.
    /// </summary>
    public RegistryKey WindowsKey { get; }

    /// <summary>
    /// Indicates whether this instance is disposed or not.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public override bool IsCaseSensitive => false;

    /// <inheritdoc/>
    public override string Name => WindowsKey.Name;

    /// <inheritdoc/>
    public override RegistryView View => ConvertView(WindowsKey.View);

    /// <summary>
    /// Creates a new <inheritdoc cref="WindowsRegistryKey"/> from a given <see cref="RegistryKey"/>.
    /// </summary>
    /// <param name="registryKey">The internal registry key this instance represents.</param>
    internal WindowsRegistryKey(RegistryKey registryKey)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Registry is not supported on this platform.");

        WindowsKey = registryKey ?? throw new ArgumentNullException(nameof(registryKey));
    }

    /// <summary>
    /// Finalizes this instance.
    /// </summary>
    ~WindowsRegistryKey() => Dispose(false);

    /// <inheritdoc />
    public override object? GetValue(string? name)
    {
        return WindowsKey.GetValue(name);
    }

    /// <inheritdoc/>
    protected override IRegistryKey? GetKeyCore(string subPath, bool writable)
    {
        var key = WindowsKey.OpenSubKey(subPath, writable);
        return key is null ? null : new WindowsRegistryKey(key);
    }

    /// <inheritdoc/>
    public override void SetValue(string? name, object value)
    {
        WindowsKey.SetValue(name, value);
    }

    /// <inheritdoc/>
    protected override void DeleteValueCore(string name)
    {
        WindowsKey.DeleteValue(name, false);
    }

    /// <inheritdoc/>
    protected override void DeleteKeyCore(string subPath, bool recursive)
    {
        if (recursive)
            WindowsKey.DeleteSubKeyTree(subPath, false);
        else
            WindowsKey.DeleteSubKey(subPath, false);
    }

    /// <inheritdoc/>
    public override IRegistryKey? CreateSubKey(string subKey)
    {
        var winKey = WindowsKey.CreateSubKey(subKey);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return winKey is null ? null : new WindowsRegistryKey(winKey);
    }

    /// <inheritdoc/>
    public override string[] GetSubKeyNames()
    {
        return WindowsKey.GetSubKeyNames();
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
            WindowsKey.Dispose();
        IsDisposed = true;
    }

    internal static Microsoft.Win32.RegistryHive ConvertHive(RegistryHive hive)
    {
        return hive switch
        {
            RegistryHive.ClassesRoot => Microsoft.Win32.RegistryHive.ClassesRoot,
            RegistryHive.LocalMachine => Microsoft.Win32.RegistryHive.LocalMachine,
            RegistryHive.CurrentUser => Microsoft.Win32.RegistryHive.CurrentUser,
            _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, null)
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