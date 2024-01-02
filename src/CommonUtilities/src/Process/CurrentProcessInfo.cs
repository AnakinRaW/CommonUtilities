#if NET
using System;
#else
using Vanara.PInvoke;
#endif
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Provides information about the current process.
/// </summary>
internal sealed class CurrentProcessInfo : ICurrentProcessInfo
{
    /// <summary>
    /// Gets an instance of <see cref="CurrentProcessInfo"/> representing the current process.
    /// </summary>
    public static readonly CurrentProcessInfo Current = new();

    /// <inheritdoc/>
    public bool IsElevated { get; }

    /// <inheritdoc/>
    public string? ProcessFilePath { get; }

    /// <inheritdoc/>
    public int Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentProcessInfo"/> struct representing the current process.
    /// </summary>
    private CurrentProcessInfo()
    {
        var p = Process.GetCurrentProcess();
        Id = p.Id;
#if NET
        var processPath = Environment.ProcessPath;
#else
        var processPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Kernel32.GetModuleFileName(HINSTANCE.NULL)
            : Process.GetCurrentProcess().MainModule.FileName;
#endif

        ProcessFilePath = processPath;

        IsElevated = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ProcessElevationWindows.IsProcessElevated()
            : ProcessElevationLinux.IsElevated();
    }
}