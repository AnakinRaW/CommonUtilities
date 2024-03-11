using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;

namespace AnakinRaW.CommonUtilities.FileSystem;

// Based on https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/FileSystem/PathUtilities.cs
// and https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Common/PathUtil/PathUtility.cs
// and https://github.com/dotnet/runtime
/// <summary>
/// Provides extension methods for the <see cref="IPath"/> class.
/// </summary>
public static class PathExtensions
{
    private const string ThisDirectory = ".";
    private const string ParentRelativeDirectory = "..";

    internal static readonly char VolumeSeparatorChar = Path.VolumeSeparatorChar;
    internal static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
    internal static readonly char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
    internal static readonly string DirectorySeparatorStr = new(DirectorySeparatorChar, 1);

    private static readonly char[] PathChars = [VolumeSeparatorChar, DirectorySeparatorChar, AltDirectorySeparatorChar];

    internal static readonly bool IsUnixLikePlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);


    internal static readonly Lazy<bool> IsFileSystemCaseInsensitive = new(CheckIfFileSystemIsCaseInsensitive);

    /// <summary>
    /// Determines whether the specified path ends with a directory path separator
    /// </summary>
    /// <param name="_"></param>
    /// <param name="path">The path to check.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> ends with a directory path separator; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static bool HasTrailingDirectorySeparator(this IPath _, string path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        return HasTrailingDirectorySeparator(path.AsSpan());
    }

    /// <summary>
    /// Determines whether the specified path ends with a directory path separator
    /// </summary>
    /// <param name="_"></param>
    /// <param name="path">The path to check.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> ends with a directory path separator; otherwise, <see langword="false"/>.</returns>
    public static bool HasTrailingDirectorySeparator(this IPath _, ReadOnlySpan<char> path)
    {
        return HasTrailingDirectorySeparator(path);
    }

    private static bool HasTrailingDirectorySeparator(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            return false;
        var last = value[value.Length - 1];
        return IsAnyDirectorySeparator(last);
    }

    private static bool IsAnyDirectorySeparator(char c)
    {
        return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
    }

    /// <summary>
    /// Checks whether a path is rooted, but not absolute, to a drive e.g, "C:" or "C:my/path"
    /// </summary>
    /// <remarks>
    /// Only works on Windows. For Linux systems, this method will always return <see langword="false"/>.
    /// </remarks>
    /// <param name="fsPath">The file system's path instance.</param>
    /// <param name="path">The path to check.</param>
    /// <param name="driveLetter">If <paramref name="path"/> is drive relative the drive's letter will be stored into this variable.
    /// <see langword="null"/> if <paramref name="path"/> is not drive relative.</param>
    /// <returns>Return <see langword="true"/> if <paramref name="path"/> is relative, but not absolute to a drive; otherwise, <see langword="false"/>.</returns>
    public static bool IsDriveRelative(this IPath fsPath, string? path, out char? driveLetter)
    {
        driveLetter = null;

        // On Linux there is no such thing as drive relative paths
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        // Implementation based on Path.Windows.cs from the .NET repository

        if (!fsPath.IsPathRooted(path))
            return false;

        if (path.Length < 2)
            return false;

        if (IsValidDriveChar(path[0]) && path[1] == VolumeSeparatorChar)
        {
            if (path.Length < 3 || !IsAnyDirectorySeparator(path[2]))
            {
                driveLetter = path[0];
                return true;
            }
            return false;
        }

        return false;
    }

    internal static string TrimTrailingSeparators(ReadOnlySpan<char> path, DirectorySeparatorKind separatorKind = DirectorySeparatorKind.System)
    {
        var lastSeparator = path.Length;
        while (lastSeparator > 0 && IsAnyDirectorySeparator(path[lastSeparator - 1], separatorKind))
            lastSeparator -= 1;
        if (lastSeparator != path.Length)
            path = path.Slice(0, lastSeparator);
        return path.ToString();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAnyDirectorySeparatorWindows(char c)
    {
        return c is '\\' or '/';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAnyDirectorySeparatorLinux(char c)
    {
        return c is '/';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAnyDirectorySeparator(char c, DirectorySeparatorKind separatorKind)
    {
        switch (separatorKind)
        {
            case DirectorySeparatorKind.System:
                return IsAnyDirectorySeparator(c);
            case DirectorySeparatorKind.Linux:
                return IsAnyDirectorySeparatorLinux(c);
            case DirectorySeparatorKind.Windows:
                return IsAnyDirectorySeparatorWindows(c);
            default:
                throw new ArgumentOutOfRangeException(nameof(separatorKind), separatorKind, null);
        }
    }

    internal static string EnsureTrailingSeparatorInternal(string input)
    {
        if (input.Length == 0 || IsAnyDirectorySeparator(input[input.Length - 1]))
            return input;

        // Use the existing slashes in the path, if they're consistent
        var hasPrimarySlash = input.IndexOf(DirectorySeparatorChar) >= 0;
        var hasAlternateSlash = input.IndexOf(AltDirectorySeparatorChar) >= 0;

        if (hasPrimarySlash && !hasAlternateSlash)
            return input + DirectorySeparatorChar;

        if (!hasPrimarySlash && hasAlternateSlash)
            return input + AltDirectorySeparatorChar;

        // If there are no slashes, or they are inconsistent, use the current platform's primary slash.
        return input + DirectorySeparatorChar;
    }


    /// <summary>
    /// Returns a relative path from a path to given root or <paramref name="path"/> if <paramref name="path"/> is not rooted.
    /// In contrast to .NET's <c>Path.GetRelativePath(string, string)</c>, if <paramref name="path"/> is not rooted this method returns <paramref name="path"/>.
    /// For any rooted path the behavior is the same.
    /// </summary>
    /// <param name="fsPath">The file system's path instance.</param>
    /// <param name="root">The root path the result should be relative to. This path is always considered to be a directory.</param>
    /// <param name="path">The destination path.</param>
    /// <returns>The relative path, or path if the paths don't share the same root.</returns>
    public static string GetRelativePathEx(this IPath fsPath, string root, string path)
    {
        var endsWithTrailingPathSeparator = HasTrailingDirectorySeparator(path.AsSpan());

        if (!fsPath.IsPathRooted(path))
            return path;

        // Root should always be absolute
        root = fsPath.GetFullPath(root);
        root = TrimTrailingSeparators(root.AsSpan());

        path = fsPath.GetFullPath(path);
        var trimmedPath = TrimTrailingSeparators(path.AsSpan());

        var rootParts = GetPathParts(root);
        var pathParts = GetPathParts(trimmedPath);

        if (rootParts.Length == 0 || pathParts.Length == 0)
            return path;

        var index = 0;

        // find index where full path diverges from base path
        var maxSearchIndex = Math.Min(rootParts.Length, pathParts.Length);
        for (; index < maxSearchIndex; index++)
        {
            if (!PathsEqual(rootParts[index], pathParts[index]))
                break;
        }

        // if the first part doesn't match, they don't even have the same volume
        // so there can be no relative path.
        if (index == 0)
            return path;

        var relativePath = string.Empty;

        // add backup notation for remaining base path levels beyond the index
        var remainingParts = rootParts.Length - index;
        if (remainingParts > 0)
        {
            for (var i = 0; i < remainingParts; i++)
                relativePath = relativePath + ParentRelativeDirectory + DirectorySeparatorStr;
        }

        if (index < pathParts.Length)
        {
            // add the rest of the full path parts
            for (var i = index; i < pathParts.Length; i++)
                relativePath = CombinePathsUnchecked(relativePath, pathParts[i]);

            if (endsWithTrailingPathSeparator)
                relativePath = EnsureTrailingSeparatorInternal(relativePath);
        }
        else
        {
            if (!string.IsNullOrEmpty(relativePath))
                relativePath = TrimTrailingSeparators(relativePath.AsSpan());
        }


        if (relativePath == string.Empty)
            return ThisDirectory;
        return relativePath;
    }

    private static string[] GetPathParts(string path)
    {
        var pathParts = path.Split(PathChars);

        // remove references to self directories ('.')
        if (pathParts.Contains(ThisDirectory))
            pathParts = pathParts.Where(s => s != ThisDirectory).ToArray();

        return pathParts;
    }

    internal static bool PathsEqual(string path1, string path2)
    {
        return PathsEqual(path1, path2, Math.Max(path1.Length, path2.Length));
    }

    private static string CombinePathsUnchecked(string root, string relativePath)
    {
        if (root == string.Empty)
            return relativePath;
        var c = root[root.Length - 1];
        if (!IsAnyDirectorySeparator(c) && c != VolumeSeparatorChar)
            return root + DirectorySeparatorStr + relativePath;
        return root + relativePath;
    }

    /// <summary>
    /// True if the two paths are the same.  (but only up to the specified length)
    /// </summary>
    private static bool PathsEqual(string path1, string path2, int length)
    {
        if (path1.Length < length || path2.Length < length)
            return false;

        for (var i = 0; i < length; i++)
        {
            if (!PathCharEqual(path1[i], path2[i]))
                return false;
        }

        return true;
    }

    private static bool PathCharEqual(char x, char y)
    {
        if (IsAnyDirectorySeparator(x) && IsAnyDirectorySeparator(y))
            return true;

        return IsFileSystemCaseInsensitive.Value
            ? char.ToUpperInvariant(x) == char.ToUpperInvariant(y)
            : x == y;
    }

    /// <summary>
    /// Determines whether a specified candidate path is a real child path to a specified base path.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="basePath">The base path-</param>
    /// <param name="candidate">The relative path candidate.</param>
    /// <returns><see langword="true"/> if <paramref name="candidate"/> is a child path to <paramref name="basePath"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsChildOf(this IPath _, string basePath, string candidate)
    {
        var fullBase = _.GetFullPath(basePath);
        var fullCandidate = _.GetFullPath(candidate);

        return fullBase.Length > 0
               && fullCandidate.Length > fullBase.Length
               && PathsEqual(fullCandidate, fullBase, fullBase.Length)
               && (IsAnyDirectorySeparator(fullBase[fullBase.Length - 1]) || IsAnyDirectorySeparator(fullCandidate[fullBase.Length]));
    }

#if !NET && !NETsta

    /// <summary>
    /// Returns <see langword="true"/> if the path specified is absolute. This method does no
    /// validation of the path.
    /// </summary>
    /// <remarks>
    /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
    /// assume that rooted paths (Path.IsPathRooted) are not relative. This isn't the case.
    /// "C:a" is drive relative- meaning that it will be resolved against the current directory
    /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
    /// will not be used to modify the path).
    /// </remarks>
    /// <param name="_"></param>
    /// <param name="path">A file path.</param>
    /// <returns><see langword="true"/> if the path is fixed to a specific drive or UNC path; <see langword="false"/> if the path is relative to the current drive or working directory.</returns>
    public static bool IsPathFullyQualified(this IPath _, ReadOnlySpan<char> path)
    {
        if (IsUnixLikePlatform)
        {
            // While this case should not get reached, we add a safeguard and fallback to the .NET routine.
            return _.IsPathRooted(path.ToString());
        }

        if (path.Length < 2)
            return false;

        if (IsAnyDirectorySeparator(path[0]))
            return path[1] == '?' || IsAnyDirectorySeparator(path[1]);

        return path.Length >= 3
               && path[1] == VolumeSeparatorChar
               && IsAnyDirectorySeparator(path[2])
               && IsValidDriveChar(path[0]);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the path specified is absolute. This method does no
    /// validation of the path.
    /// </summary>
    /// <remarks>
    /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
    /// assume that rooted paths (Path.IsPathRooted) are not relative. This isn't the case.
    /// "C:a" is drive relative-meaning that it will be resolved against the current directory
    /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
    /// will not be used to modify the path).
    /// </remarks>
    /// <param name="_"></param>
    /// <param name="path">A file path.</param>
    /// <returns><see langword="true"/> if the path is fixed to a specific drive or UNC path; <see langword="false"/> if the path is relative to the current drive or working directory.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/>is <see langword="null"/>.</exception>
    public static bool IsPathFullyQualified(this IPath _, string path)
    {
        if (path == null) 
            throw new ArgumentNullException(nameof(path));
        return IsPathFullyQualified(_, path.AsSpan());
    }

#endif


    internal static bool IsValidDriveChar(char value)
    {
        return (uint)((value | 0x20) - 'a') <= 'z' - 'a';
    }
    
#pragma warning disable IO0003
#pragma warning disable IO0006
    private static bool CheckIfFileSystemIsCaseInsensitive()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return true;
        var listOfPathsToCheck = new[]
        {
            Path.GetTempPath(),
            Directory.GetCurrentDirectory()
        };

        var isCaseInsensitive = true;
        foreach (var path in listOfPathsToCheck)
        {
            var result = CheckCaseSensitivityRecursivelyTillDirectoryExists(path, out var ignore);
            if (!ignore)
                isCaseInsensitive &= result;
        }
        return isCaseInsensitive;
    }

    private static bool CheckCaseSensitivityRecursivelyTillDirectoryExists(string path, out bool ignoreResult)
    {
        path = Path.GetFullPath(path);
        ignoreResult = true;
        var parentDirectoryFound = true;
        while (true)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.Length <= 1)
            {
                ignoreResult = true;
                parentDirectoryFound = false;
                break;
            }
            if (Directory.Exists(path))
            {
                ignoreResult = false;
                break;
            }
            path = Path.GetDirectoryName(path)!;
        }

        if (parentDirectoryFound)
        {
            return Directory.Exists(path.ToLowerInvariant()) && Directory.Exists(path.ToUpperInvariant());
        }
        return false;
    }
#pragma warning restore IO0003
#pragma warning restore IO0006

}