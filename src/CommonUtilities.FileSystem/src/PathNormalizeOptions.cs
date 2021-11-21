using System;

namespace Sklavenwalker.CommonUtilities.FileSystem;

/// <summary>
/// Options which can be performed by an path normalization.
/// </summary>
[Flags]
public enum PathNormalizeOptions
{
    /// <summary>
    /// Directory Separator Characters get unified across the path based on the system's preferred separator.
    /// <remarks>On Windows this is '\'. On Linux this is '/'</remarks>
    /// </summary>
    UnifySlashes = 1,
    /// <summary>
    /// Removes any trailing directory separators at the end of a path.
    /// </summary>
    TrimTrailingSeparator = 2,
    /// <summary>
    /// On case insensitive systems (like Windows) the normalization will lower all characters.
    /// </summary>
    ToLowerCase = 4,
    /// <summary>
    /// Makes a path absolute and resolves any '..' and '.' path identifiers.
    /// </summary>
    ResolveFullPath = 8,
    /// <summary>
    /// Removes adjacent directory separators.
    /// <remarks>This options is not necessary when <see cref="ResolveFullPath"/> is applied.</remarks>
    /// </summary>
    RemoveAdjacentSlashes = 16,
    /// <summary>
    /// Applies all available options.
    /// </summary>
    Full = UnifySlashes | TrimTrailingSeparator | ToLowerCase | ResolveFullPath,
    /// <summary>
    /// Applies all available options excluding <see cref="ResolveFullPath"/>.
    /// </summary>
    FullNoResolve = UnifySlashes | TrimTrailingSeparator | ToLowerCase | RemoveAdjacentSlashes
}