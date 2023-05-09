using System;
using System.Diagnostics;
#if !NET6_0
using System.Runtime.InteropServices;
using Vanara.PInvoke;
#endif

namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Provides information about the current process, such as its file path and process ID.
/// </summary>
public readonly struct CurrentProcessInfo
{
    /// <summary>
    /// Gets an instance of <see cref="CurrentProcessInfo"/> representing the current process.
    /// </summary>
    public static readonly CurrentProcessInfo Current = new();

    /// <summary>
    /// Gets the file path of the current process, or null if the path could not be determined.
    /// </summary>
    public readonly string? ProcessFilePath;

    /// <summary>
    /// Gets the ID of the current process.
    /// </summary>
    public readonly int Id;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentProcessInfo"/> struct representing the current process.
    /// </summary>
    public CurrentProcessInfo()
    {
        var p = Process.GetCurrentProcess();
        Id = p.Id;
#if NET6_0
        var processPath = Environment.ProcessPath;
#else
        var processPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Kernel32.GetModuleFileName(HINSTANCE.NULL)
            : Process.GetCurrentProcess().MainModule.FileName;
#endif
        if (string.IsNullOrEmpty(processPath))
            throw new InvalidOperationException("Unable to get current process path");
        ProcessFilePath = processPath!;
    }
}