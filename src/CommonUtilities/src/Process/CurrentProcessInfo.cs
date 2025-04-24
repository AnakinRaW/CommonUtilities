#if !NET
using AnakinRaW.CommonUtilities.NativeMethods;
#endif
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Provides information about the current process.
/// </summary>
public sealed class CurrentProcessInfo : ICurrentProcessInfo
{
    private readonly Lazy<bool> _isElevatedLazy;

    /// <summary>
    /// Gets an instance of <see cref="CurrentProcessInfo"/> representing the current process.
    /// </summary>
    public static readonly CurrentProcessInfo Current = new();

    /// <inheritdoc/>
    public bool IsElevated => _isElevatedLazy.Value;

    /// <inheritdoc/>
    public string? ProcessFilePath { get; }

    /// <inheritdoc/>
    public int Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentProcessInfo"/> struct representing the current process.
    /// </summary>
    private CurrentProcessInfo()
    {
        using var p = Process.GetCurrentProcess();
        Id = p.Id;
#if NET6_0_OR_GREATER
        var processPath = Environment.ProcessPath;
#else
        var processPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Kernel32.GetModuleFileName(IntPtr.Zero)
            : p.MainModule!.FileName;
#endif
        ProcessFilePath = processPath;
        _isElevatedLazy = new Lazy<bool>(CheckIsElevated);
    }

    private bool CheckIsElevated()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ProcessElevationWindows.IsProcessElevated(Id)
            : ProcessElevationLinux.IsElevated();
    }
}