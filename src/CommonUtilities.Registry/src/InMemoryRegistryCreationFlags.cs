using System;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// Specifies the behavior of the <see cref="InMemoryRegistry"/>.
/// </summary>
[Flags]
public enum InMemoryRegistryCreationFlags
{
    /// <summary>
    /// By default, the <see cref="InMemoryRegistry"/> is case-insensitive and does not mimic any Windows Registry specialities
    /// such as key and value name length or forbidden data types.
    /// </summary>
    Default = 0,
    /// <summary>
    /// The <see cref="InMemoryRegistry"/> shall treat key and value names in a case-sensitive manner.
    /// </summary>
    CaseSensitive = 1,
    /// <summary>
    /// The <see cref="InMemoryRegistry"/> uses the same key and value name length limitations as the Windows Registry.
    /// For keys the max. name length is 255 .For values the max. name length is 16,383.
    /// </summary>
    UseWindowsLengthLimits = 2,
    /// <summary>
    /// The <see cref="InMemoryRegistry"/> only supports <see cref="string"/> and <see cref="byte"/> as array types.
    /// String arrays must not contain <see langword="null"/> references.
    /// </summary>
    OnlyUseWindowsDataTypes = 4,
    /// <summary>
    /// The <see cref="InMemoryRegistry"/> behaves like the Windows Registry does.
    /// </summary>
    WindowsLike = UseWindowsLengthLimits | OnlyUseWindowsDataTypes
}