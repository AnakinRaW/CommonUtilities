using System;
using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities;

/// <inheritdoc/>
public sealed class ProcessElevation : IProcessElevation
{
    /// <summary>
    /// The default instance of the <see cref="IProcessElevation"/> service.
    /// </summary>
    public static readonly IProcessElevation Default = new ProcessElevation();

    private readonly Lazy<bool> _elevatedLazy;

    /// <inheritdoc/>
    public bool IsCurrentProcessElevated => _elevatedLazy.Value;

    private ProcessElevation()
    {
        _elevatedLazy = new Lazy<bool>(IsProcessElevated);
    }

    private bool IsProcessElevated()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ProcessElevationWindows.IsProcessElevated()
            : ProcessElevationLinux.IsElevated();
    }
}