﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using AnakinRaW.CommonUtilities.FileSystem.Utilities;

namespace AnakinRaW.CommonUtilities.FileSystem;

// ReSharper disable once PartialTypeWithSinglePart
// Based on https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/FileSystem/PathUtilities.cs
// and https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Common/PathUtil/PathUtility.cs
// and https://github.com/dotnet/runtime
/// <summary>
/// Provides extension methods for the <see cref="IPath"/> class.
/// </summary>
public static partial class PathExtensions
{
    private const string ThisDirectory = ".";
    private const string ParentRelativeDirectory = "..";

    internal const int MaxShortPath = 260;

    internal static readonly char VolumeSeparatorChar = Path.VolumeSeparatorChar;
    internal static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
    internal static readonly char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
    internal static readonly string DirectorySeparatorCharAsString = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";

    private static readonly char[] PathChars = [VolumeSeparatorChar, DirectorySeparatorChar, AltDirectorySeparatorChar];

    internal static readonly bool IsUnixLikePlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);


    internal static readonly Lazy<bool> IsFileSystemCaseInsensitive = new(CheckIfFileSystemIsCaseInsensitive);

    /// <summary>
    /// Returns a value that indicates whether the path ends in a directory separator.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="path">The path to analyze.</param>
    /// <returns><see langword="true"/> if the path ends in a directory separator; otherwise, <see langword="false"/>.</returns>
    public static bool HasTrailingDirectorySeparator(this IPath _, [NotNullWhen(true)] string? path)
    {
        return path is not null && HasTrailingDirectorySeparator(path.AsSpan());
    }

    /// <summary>
    /// Returns a value that indicates whether the path, specified as a read-only span, ends in a directory separator.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="path">The path to analyze.</param>
    /// <returns><see langword="true"/> if the path ends in a directory separator; otherwise, <see langword="false"/>.</returns>
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

    /// <summary>
    /// Returns a value that indicates whether the path starts with a directory separator.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="path">The path to analyze.</param>
    /// <returns><see langword="true"/> if the path starts with a directory separator; otherwise, <see langword="false"/>.</returns>
    public static bool HasLeadingDirectorySeparator(this IPath _, [NotNullWhen(true)] string? path)
    {
        return path is not null && HasLeadingDirectorySeparator(path.AsSpan());
    }

    /// <summary>
    /// Returns a value that indicates whether the path, specified as a read-only span, starts with a directory separator.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="path">The path to analyze.</param>
    /// <returns><see langword="true"/> if the path starts with a directory separator; otherwise, <see langword="false"/>.</returns>
    public static bool HasLeadingDirectorySeparator(this IPath _, ReadOnlySpan<char> path)
    {
        return HasLeadingDirectorySeparator(path);
    }

    private static bool HasLeadingDirectorySeparator(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            return false;
        var first = value[0];
        return IsAnyDirectorySeparator(first);
    }

    private static bool IsAnyDirectorySeparator(char c)
    {
        return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
    }

    /// <summary>
    /// Checks whether a character span is rooted, but not absolute, to a drive e.g, "C:" or "C:my/path"
    /// </summary>
    /// <remarks>
    /// Only works on Windows. For Linux systems, this method will always return <see langword="false"/>.
    /// </remarks>
    /// <param name="fsPath">The file system's path instance.</param>
    /// <param name="path">The character span to check.</param>
    /// <param name="driveLetter">If <paramref name="path"/> is drive relative the drive's letter will be stored into this variable.
    /// <see langword="null"/> if <paramref name="path"/> is not drive relative.</param>
    /// <returns>Return <see langword="true"/> if <paramref name="path"/> is relative, but not absolute to a drive; otherwise, <see langword="false"/>.</returns>
    public static bool IsDriveRelative(this IPath fsPath, ReadOnlySpan<char> path, [NotNullWhen(true)] out char? driveLetter)
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
    public static bool IsDriveRelative(this IPath fsPath, string? path, [NotNullWhen(true)] out char? driveLetter)
    {
        return fsPath.IsDriveRelative(path.AsSpan(), out driveLetter);
    }

    internal static ReadOnlySpan<char> TrimTrailingSeparators(ReadOnlySpan<char> path, DirectorySeparatorKind separatorKind = DirectorySeparatorKind.System)
    {
        var lastSeparator = path.Length;
        while (lastSeparator > 0 && IsAnyDirectorySeparator(path[lastSeparator - 1], separatorKind))
            lastSeparator -= 1;
        return lastSeparator != path.Length ? path.Slice(0, lastSeparator) : path;
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


    internal static void EnsureTrailingSeparatorInternal(ref ValueStringBuilder stringBuilder)
    {
        if (stringBuilder.Length == 0 || IsAnyDirectorySeparator(stringBuilder[stringBuilder.Length - 1]))
            return;

        // Use the existing slashes in the path, if they're consistent
        var hasPrimarySlash = stringBuilder.RawChars.IndexOf(DirectorySeparatorChar) >= 0;
        var hasAlternateSlash = stringBuilder.RawChars.IndexOf(AltDirectorySeparatorChar) >= 0;

        if (!hasPrimarySlash && hasAlternateSlash)
        {
            stringBuilder.Append(AltDirectorySeparatorChar);
            return;
        }

        stringBuilder.Append(DirectorySeparatorChar);
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
        root = TrimTrailingSeparators(root.AsSpan()).ToString();

        path = fsPath.GetFullPath(path);
        var trimmedPath = TrimTrailingSeparators(path.AsSpan()).ToString();

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

        var sb = new ValueStringBuilder(stackalloc char[MaxShortPath]);

        // add backup notation for remaining base path levels beyond the index
        var remainingParts = rootParts.Length - index;
        if (remainingParts > 0)
        {
            for (var i = 0; i < remainingParts; i++)
            {
                sb.Append(ParentRelativeDirectory);
                sb.Append(DirectorySeparatorChar);
            }
        }

        if (index < pathParts.Length)
        {
            // add the rest of the full path parts
            for (var i = index; i < pathParts.Length; i++)
                CombinePathsUnchecked(ref sb, pathParts[i]);

            if (endsWithTrailingPathSeparator) 
                EnsureTrailingSeparatorInternal(ref sb);
        }
        else
        {
            if (sb.Length > 0)
            {
                if (HasTrailingDirectorySeparator(sb.AsSpan()))
                    sb.Length -= 1;
            }
        }


        if (sb.Length == 0)
            return ThisDirectory;

        var result = sb.ToString();
        sb.Dispose();
        return result;
    }

    private static string[] GetPathParts(string path)
    {
        var pathParts = path.Split(PathChars);

        // remove references to self directories ('.')
        if (pathParts.Contains(ThisDirectory))
            pathParts = pathParts.Where(s => s != ThisDirectory).ToArray();

        return pathParts;
    }

    /// <summary>
    /// Determines whether two specified paths are equal.
    /// </summary>
    /// <remarks>
    /// This method resolves the full paths of <paramref name="pathA"/> and <paramref name="pathB"/> and checks whether they
    /// match under the rules of the current file system. This includes character casing and directory separator variants.
    /// </remarks>
    /// <param name="_">The file system's path instance.</param>
    /// <param name="pathA">The first path to compare</param>
    /// <param name="pathB">The second path to compare</param>
    /// <returns><see langword="true"/> if both <paramref name="pathA"/> and <paramref name="pathB"/> are equal on this system; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pathA"/> or <paramref name="pathB"/> is <see langword="null"/>.</exception>
    public static bool AreEqual(this IPath _, string pathA, string pathB)
    {
        return PathsEqual(_.GetFullPath(pathA), _.GetFullPath(pathB));
    }

#if !NET9_0_OR_GREATER

    /// <summary>
    /// Concatenates a span of paths into a single path.
    /// </summary>
    /// <param name="_">The file system's path instance.</param>
    /// <param name="paths">A span of paths.</param>
    /// <returns>The concatenated path.</returns>
    public static string Join(this IPath _, params ReadOnlySpan<string?> paths)
    {
        if (paths.IsEmpty)
            return string.Empty;

        var maxSize = 0;
        foreach (var path in paths) 
            maxSize += path?.Length ?? 0;
        maxSize += paths.Length - 1;

        var builder = new ValueStringBuilder(stackalloc char[260]); // MaxShortPath on Windows
        builder.EnsureCapacity(maxSize);

        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path))
                continue;
            if (builder.Length != 0)
            {
                if (!IsAnyDirectorySeparator(builder[builder.Length - 1]) && !IsAnyDirectorySeparator(path![0]))
                    builder.Append(_.DirectorySeparatorChar);
            }

            builder.Append(path);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Combines a span of strings into a path.
    /// </summary>
    /// <param name="_">The file system's path instance.</param>
    /// <param name="paths">A span of parts of the path.</param>
    /// <returns>The combined paths.</returns>
    /// <exception cref="ArgumentNullException">One of the strings in the span is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// .NET Framework and .NET Core versions older than 2.1:
    /// One of the strings in the span contains one or more of the invalid characters defined in <see cref="IPath.GetInvalidPathChars"/>
    /// .</exception>
    public static string Combine(this IPath _, params ReadOnlySpan<string> paths)
    {
        var maxSize = 0;
        var firstComponent = 0;

        // We have two passes, the first calculates how large a buffer to allocate and does some precondition
        // checks on the paths passed in.  The second actually does the combination.

        for (var i = 0; i < paths.Length; i++)
        {
            var segment = paths[i];
            ThrowHelper.ThrowIfNull(segment, nameof(paths));

            if (segment.Length == 0)
                continue;

            if (_.IsPathRooted(segment))
            {
                firstComponent = i;
                maxSize = segment.Length;
            }
            else
                maxSize += segment.Length;

            var ch = segment[segment.Length - 1];
            if (!IsAnyDirectorySeparator(ch))
                maxSize++;
        }

        var builder = new ValueStringBuilder(stackalloc char[260]); // MaxShortPath on Windows
        builder.EnsureCapacity(maxSize);

        for (var i = firstComponent; i < paths.Length; i++)
        {
            var segment = paths[i];
            if (segment.Length == 0)
                continue;

            if (builder.Length == 0)
                builder.Append(segment);
            else
            {
                var ch = builder[builder.Length - 1];
                if (!IsAnyDirectorySeparator(ch)) 
                    builder.Append(_.DirectorySeparatorChar);

                builder.Append(segment);
            }
        }

        return builder.ToString();
    }

#endif

    internal static bool PathsEqual(string path1, string path2)
    {
        return PathsEqual(path1.AsSpan(), path2.AsSpan(), Math.Max(path1.Length, path2.Length));
    }

    private static void CombinePathsUnchecked(ref ValueStringBuilder sb, string relativePath)
    {
        if (sb.Length == 0)
        {
            sb.Append(relativePath);
            return;
        }
        var c = sb[sb.Length - 1];
        if (!IsAnyDirectorySeparator(c) && c != VolumeSeparatorChar) 
            sb.Append(DirectorySeparatorChar);
        sb.Append(relativePath);
    }

    /// <summary>
    /// True if the two paths are the same.  (but only up to the specified length)
    /// </summary>
    private static bool PathsEqual(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, int length)
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
        return _.IsChildOf(fullBase.AsSpan(), fullCandidate.AsSpan());
    }

    /// <summary>
    /// Determines whether a specified candidate path is a real child path to a specified base path.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="basePath">The base path-</param>
    /// <param name="candidate">The relative path candidate.</param>
    /// <returns><see langword="true"/> if <paramref name="candidate"/> is a child path to <paramref name="basePath"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="basePath"/> or <paramref name="candidate"/> are not fully qualified.</exception>
    public static bool IsChildOf(this IPath _, ReadOnlySpan<char> basePath, ReadOnlySpan<char> candidate)
    {
        if (!_.IsPathFullyQualified(basePath))
            throw new ArgumentException("candidate must be fully qualified", nameof(basePath));
        if (!_.IsPathFullyQualified(candidate))
            throw new ArgumentException("candidate must be fully qualified", nameof(candidate));
        
        return basePath.Length > 0
               && candidate.Length > basePath.Length
               && PathsEqual(candidate, basePath, basePath.Length)
               && (IsAnyDirectorySeparator(basePath[basePath.Length - 1]) || IsAnyDirectorySeparator(candidate[basePath.Length]));
    }

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